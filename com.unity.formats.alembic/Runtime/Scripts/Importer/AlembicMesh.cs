using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Rendering;

namespace UnityEngine.Formats.Alembic.Importer
{
    internal class AlembicMesh : AlembicElement
    {
        internal class Submesh : IDisposable
        {
            public PinnedList<int> indexes = new PinnedList<int>();
            public bool update = true;

            public void Dispose()
            {
                if (indexes != null) indexes.Dispose();
            }
        }

        internal class Split : IDisposable
        {
            public NativeArray<Vector3> points;
            public NativeArray<Vector3> velocities;
            public NativeArray<Vector3> normals;
            public NativeArray<Vector4> tangents;
            public NativeArray<Vector2> uv0;
            public NativeArray<Vector2> uv1;
            public NativeArray<Color> rgba;
            public NativeArray<Color> rgb;

            public Mesh mesh;
            public GameObject host;
            public bool active = true;

            public void Dispose()
            {
                points.DisposeIfNeeded();
                velocities.DisposeIfNeeded();
                normals.DisposeIfNeeded();
                tangents.DisposeIfNeeded();
                uv0.DisposeIfNeeded();
                uv1.DisposeIfNeeded();
                rgba.DisposeIfNeeded();
                rgb.DisposeIfNeeded();
                if ((mesh.hideFlags & HideFlags.DontSave) != 0)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(mesh);
#else
                    Object.Destroy(mesh);
#endif
                    mesh = null;
                }
            }
        }

        aiPolyMesh m_abcSchema;
        aiMeshSummary m_summary;
        aiMeshSampleSummary m_sampleSummary;
        NativeArray<aiMeshSplitSummary> m_splitSummaries;
        NativeArray<aiSubmeshSummary> m_submeshSummaries;
        NativeArray<aiPolyMeshData> m_splitData;
        NativeArray<aiSubmeshData> m_submeshData;

        JobHandle fillVertexBufferHandle;

        List<Split> m_splits = new List<Split>();
        List<Submesh> m_submeshes = new List<Submesh>();

        internal override aiSchema abcSchema { get { return m_abcSchema; } }
        public override bool visibility { get { return m_sampleSummary.visibility; } }

        public aiMeshSummary summary { get { return m_summary; } }
        public aiMeshSampleSummary sampleSummary { get { return m_sampleSummary; } }

        protected override void Dispose(bool v)
        {
            base.Dispose(v);
            foreach (var split in m_splits)
            {
                split.Dispose();
            }

            m_splitSummaries.Dispose();
            m_submeshSummaries.Dispose();;
            m_splitData.Dispose();;
            m_submeshData.Dispose();;
        }

        void UpdateSplits(int numSplits)
        {
            Split split = null;

            if (m_summary.topologyVariance == aiTopologyVariance.Heterogeneous || numSplits > 1)
            {
                for (int i = 0; i < numSplits; ++i)
                {
                    if (i >= m_splits.Count)
                        m_splits.Add(new Split());
                    else
                        m_splits[i].active = true;
                }
            }
            else
            {
                if (m_splits.Count == 0)
                {
                    split = new Split
                    {
                        host = abcTreeNode.gameObject,
                    };
                    m_splits.Add(split);
                }
                else
                {
                    m_splits[0].active = true;
                }
            }

            for (int i = numSplits; i < m_splits.Count; ++i)
                m_splits[i].active = false;
        }

        internal override void AbcSetup(aiObject abcObj, aiSchema abcSchema)
        {
            base.AbcSetup(abcObj, abcSchema);
            m_abcSchema = (aiPolyMesh)abcSchema;

            m_abcSchema.GetSummary(ref m_summary);
        }

        public override void AbcSyncDataBegin()
        {
            if (!m_abcSchema.schema.isDataUpdated)
                return;

            var sample = m_abcSchema.sample;

            sample.GetSummary(ref m_sampleSummary);
            int splitCount = m_sampleSummary.splitCount;
            int submeshCount = m_sampleSummary.submeshCount;

            m_splitSummaries.ResizeIfNeeded(splitCount);
            m_splitData.ResizeIfNeeded(splitCount);
            m_submeshSummaries.ResizeIfNeeded(submeshCount);
            m_submeshData.ResizeIfNeeded(submeshCount);


            sample.GetSplitSummaries(m_splitSummaries);
            sample.GetSubmeshSummaries(m_submeshSummaries);

            UpdateSplits(splitCount);

            bool topologyChanged = m_sampleSummary.topologyChanged;

            // setup buffers
            var vertexData = default(aiPolyMeshData);
            for (int spi = 0; spi < splitCount; ++spi)
            {
                var split = m_splits[spi];
                split.active = true;

                int vertexCount = m_splitSummaries[spi].vertexCount;

                if (!m_summary.constantPoints || topologyChanged)
                    split.points.ResizeIfNeeded(vertexCount);
                else
                    split.points.ResizeIfNeeded(0);
                vertexData.positions = split.points.GetUnsafePointer();

                if (m_summary.hasVelocities && (!m_summary.constantVelocities || topologyChanged))
                    split.velocities.ResizeIfNeeded(vertexCount);
                else
                    split.velocities.ResizeIfNeeded(0);
                vertexData.velocities = split.velocities.GetUnsafePointer();

                if (m_summary.hasNormals && (!m_summary.constantNormals || topologyChanged))
                    split.normals.ResizeIfNeeded(vertexCount);
                else
                    split.normals.ResizeIfNeeded(0);
                vertexData.normals = split.normals.GetUnsafePointer();

                if (m_summary.hasTangents && (!m_summary.constantTangents || topologyChanged))
                    split.tangents.ResizeIfNeeded(vertexCount);
                else
                    split.tangents.ResizeIfNeeded(0);
                vertexData.tangents = split.tangents.GetUnsafePointer();

                if (m_summary.hasUV0 && (!m_summary.constantUV0 || topologyChanged))
                    split.uv0.ResizeIfNeeded(vertexCount);
                else
                    split.uv0.ResizeIfNeeded(0);
                vertexData.uv0 = split.uv0.GetUnsafePointer();

                if (m_summary.hasUV1 && (!m_summary.constantUV1 || topologyChanged))
                    split.uv1.ResizeIfNeeded(vertexCount);
                else
                    split.uv1.ResizeIfNeeded(0);
                vertexData.uv1 = split.uv1.GetUnsafePointer();

                if (m_summary.hasRgba && (!m_summary.constantRgba || topologyChanged))
                    split.rgba.ResizeIfNeeded(vertexCount);
                else
                    split.rgba.ResizeIfNeeded(0);
                vertexData.rgba = split.rgba.GetUnsafePointer();

                if (m_summary.hasRgb && (!m_summary.constantRgb || topologyChanged)) {
                    split.rgb.ResizeIfNeeded(vertexCount);
                } else {
                    split.rgb.ResizeIfNeeded(0);
                }
                vertexData.rgb = split.rgb.GetUnsafePointer();

                m_splitData[spi] = vertexData;
            }

            {
                if (m_submeshes.Count > submeshCount)
                    m_submeshes.RemoveRange(submeshCount, m_submeshes.Count - submeshCount);
                while (m_submeshes.Count < submeshCount)
                    m_submeshes.Add(new Submesh());

                var submeshData = default(aiSubmeshData);
                for (int smi = 0; smi < submeshCount; ++smi)
                {
                    var submesh = m_submeshes[smi];
                    m_submeshes[smi].update = true;
                    submesh.indexes.ResizeDiscard(m_submeshSummaries[smi].indexCount);
                    submeshData.indexes = submesh.indexes;
                    m_submeshData[smi] = submeshData;
                }
            }

            var job = new FillVertexBufferJob {sample = sample, splitData = m_splitData, submeshData = m_submeshData};

            fillVertexBufferHandle = job.Schedule();
        }

        struct FillVertexBufferJob : IJob
        {
            public aiPolyMeshSample sample;
            public NativeArray<aiPolyMeshData> splitData;
            public NativeArray<aiSubmeshData> submeshData;
            public void Execute()
            {
                sample.FillVertexBuffer(splitData, submeshData);
            }
        }

        public override void AbcSyncDataEnd()
        {
            fillVertexBufferHandle.Complete();
#if UNITY_EDITOR
            for (int s = 0; s < m_splits.Count; ++s)
            {
                var split = m_splits[s];
                if (split.host == null)
                    continue;

                var mf = split.host.GetComponent<MeshFilter>();
                if (mf != null)
                    mf.sharedMesh = split.mesh;
            }
#endif

            if (!m_abcSchema.schema.isDataUpdated)
                return;

            bool topologyChanged = m_sampleSummary.topologyChanged;
            if (abcTreeNode.stream.streamDescriptor.Settings.ImportVisibility)
            {
                var visible = m_sampleSummary.visibility;
                abcTreeNode.gameObject.SetActive(visible);
                if (!visible && !topologyChanged)
                    return;
            }

            bool useSubObjects = m_sampleSummary.splitCount > 1;

            for (int s = 0; s < m_splits.Count; ++s)
            {
                var split = m_splits[s];
                if (split.active)
                {
                    // Feshly created splits may not have their host set yet
                    if (split.host == null)
                    {
                        if (useSubObjects)
                        {
                            string name = abcTreeNode.gameObject.name + "_split_" + s;

                            Transform trans = abcTreeNode.gameObject.transform.Find(name);

                            if (trans == null)
                            {
                                GameObject go = new GameObject();
                                go.name = name;

                                trans = go.GetComponent<Transform>();
                                trans.parent = abcTreeNode.gameObject.transform;
                                trans.localPosition = Vector3.zero;
                                trans.localEulerAngles = Vector3.zero;
                                trans.localScale = Vector3.one;
                            }

                            split.host = trans.gameObject;
                        }
                        else
                        {
                            split.host = abcTreeNode.gameObject;
                        }
                    }

                    // Feshly created splits may not have their mesh set yet
                    if (split.mesh == null)
                        split.mesh = AddMeshComponents(split.host);
                    if (topologyChanged)
                    {
                        split.mesh.Clear();
                        split.mesh.subMeshCount = m_splitSummaries[s].submeshCount;
                    }

                    if (split.points.Length > 0)
                        split.mesh.SetVertices(split.points);
                    if (split.normals.Length > 0)
                        split.mesh.SetNormals(split.normals);
                    if (split.tangents.Length > 0)
                        split.mesh.SetTangents(split.tangents);
                    if (split.uv0.Length > 0)
                        split.mesh.SetUVs(0, split.uv0);
                    if (split.uv1.Length > 0)
                        split.mesh.SetUVs(1, split.uv1);
                    if (split.velocities.Length > 0)
                        split.mesh.SetUVs(5, split.velocities);


                    if (split.rgba.Length > 0)
                        split.mesh.SetColors(split.rgba);
                    else if (split.rgb.Length > 0)
                        split.mesh.SetColors(split.rgb);

                    // update the bounds
                    var data = m_splitData[s];
                    split.mesh.bounds = new Bounds(data.center, data.extents);

                    split.host.SetActive(true);
                }
                else
                {
                    split.host.SetActive(false);
                }
            }

            for (int smi = 0; smi < m_sampleSummary.submeshCount; ++smi)
            {
                var submesh = m_submeshes[smi];
                if (submesh.update)
                {
                    var sum = m_submeshSummaries[smi];
                    var split = m_splits[sum.splitIndex];
                    if (sum.topology == aiTopology.Triangles)
                        split.mesh.SetTriangles(submesh.indexes.List, sum.submeshIndex, false);
                    else if (sum.topology == aiTopology.Lines)
                        split.mesh.SetIndices(submesh.indexes.GetArray(), MeshTopology.Lines, sum.submeshIndex, false);
                    else if (sum.topology == aiTopology.Points)
                        split.mesh.SetIndices(submesh.indexes.GetArray(), MeshTopology.Points, sum.submeshIndex, false);
                    else if (sum.topology == aiTopology.Quads)
                        split.mesh.SetIndices(submesh.indexes.GetArray(), MeshTopology.Quads, sum.submeshIndex, false);
                }
            }
        }

        Mesh AddMeshComponents(GameObject go)
        {
            Mesh mesh = null;
            var meshFilter = go.GetComponent<MeshFilter>();
            bool hasMesh = meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.name.IndexOf("dyn: ") == 0;

            if (!hasMesh)
            {
                mesh = new Mesh { name = "dyn: " + go.name };
#if UNITY_2017_3_OR_NEWER
                mesh.indexFormat = IndexFormat.UInt32;
#endif
                mesh.MarkDynamic();

                if (meshFilter == null)
                    meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    renderer = go.AddComponent<MeshRenderer>();
                    var material = go.transform.parent.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                    renderer.sharedMaterial = material;
                }
            }
            else
            {
                mesh = UnityEngine.Object.Instantiate(meshFilter.sharedMesh);
                meshFilter.sharedMesh = mesh;
                mesh.name = "dyn: " + go.name;
            }

            return mesh;
        }
    }
}
