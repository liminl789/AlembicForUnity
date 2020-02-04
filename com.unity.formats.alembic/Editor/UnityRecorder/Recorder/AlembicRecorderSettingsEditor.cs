#if RECORDER_AVAILABLE
using UnityEditor.Formats.Alembic.Exporter;
using UnityEditor.Recorder;

namespace UnityEditor.Formats.Alembic.Recorder
{
    [CustomEditor(typeof(AlembicRecorderSettings))]
    public class AlembicRecorderSettingsEditor : RecorderEditor
    {
        bool m_foldCaptureComponents;
        bool m_foldMeshComponents;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var t = target as AlembicRecorderSettings;
            var dirty = AlembicExporterEditor.DrawSettings(serializedObject,
                t.Settings,
                "settings.", ref m_foldCaptureComponents, ref m_foldMeshComponents, true);

            serializedObject.ApplyModifiedProperties();
            if (dirty)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
