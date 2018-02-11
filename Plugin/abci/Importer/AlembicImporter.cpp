﻿#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"
#include "aiPolyMesh.h"
#include "aiCamera.h"
#include "aiPoints.h"
#include "aiProperty.h"

abciAPI abcSampleSelector aiTimeToSampleSelector(double time)
{
    return abcSampleSelector(time, Abc::ISampleSelector::kFloorIndex);
}

abciAPI abcSampleSelector aiIndexToSampleSelector(int64_t index)
{
    return abcSampleSelector(index);
}

abciAPI void aiCleanup()
{
}

abciAPI void aiClearContextsWithPath(const char *path)
{
    aiContextManager::destroyContextsWithPath(path);
}

abciAPI aiContext* aiContextCreate(int uid)
{
    return aiContextManager::getContext(uid);
}

abciAPI void aiContextDestroy(aiContext* ctx)
{
    if (ctx)
        aiContextManager::destroyContext(ctx->getUid());
}


abciAPI bool aiContextLoad(aiContext* ctx, const char *path)
{
    return ctx ? ctx->load(path) : false;
}

abciAPI void aiContextSetConfig(aiContext* ctx, const aiConfig* conf)
{
    if (ctx)
        ctx->setConfig(*conf);
}

abciAPI int aiContextGetTimeRangeCount(aiContext* ctx)
{
    if (ctx)
        return ctx->getTimeSamplingCount();
    return 0;
}

abciAPI void aiContextGetTimeRange(aiContext* ctx, int i, aiTimeRange *dst)
{
    if(ctx && dst)
        ctx->getTimeRange(i, *dst);
}

abciAPI aiObject* aiContextGetTopObject(aiContext* ctx)
{
    return ctx ? ctx->getTopObject() : 0;
}

abciAPI void aiContextUpdateSamples(aiContext* ctx, double time)
{
    if (ctx)
        ctx->updateSamples(time);
}

abciAPI const char* aiObjectGetName(aiObject* obj)
{
    return obj ? obj->getName() : "";
}

abciAPI const char* aiGetFullNameS(aiObject* obj)
{
    return obj ? obj->getFullName() : "";
}

abciAPI int aiObjectGetNumChildren(aiObject* obj)
{
    return obj ? obj->getNumChildren() : 0;
}

abciAPI aiObject* aiObjectGetChild(aiObject* obj, int i)
{
    return obj ? obj->getChild(i) : nullptr;
}

abciAPI aiObject* aiObjectGetParent(aiObject* obj)
{
    return obj ? obj->getParent() : nullptr;
}

abciAPI void aiObjectSetEnabled(aiObject * obj, bool v)
{
    if (obj)
        obj->setEnabled(v);
}

abciAPI aiXform* aiObjectAsXform(aiObject* obj)
{
    return obj ? dynamic_cast<aiXform*>(obj) : nullptr;
}

abciAPI aiPolyMesh* aiObjectAsPolyMesh(aiObject* obj)
{
    return obj ? dynamic_cast<aiPolyMesh*>(obj) : nullptr;
}

abciAPI aiCamera* aiObjectAsCamera(aiObject* obj)
{
    return obj ? dynamic_cast<aiCamera*>(obj) : nullptr;
}

abciAPI aiPoints* aiObjectAsPoints(aiObject* obj)
{
    return obj ? dynamic_cast<aiPoints*>(obj) : nullptr;
}


abciAPI aiSample* aiSchemaGetSample(aiSchema * schema)
{
    return schema ? schema->getSample() : nullptr;
}

abciAPI void aiSchemaUpdateSample(aiSchema* schema, const abcSampleSelector *ss)
{
    if (schema) {
        schema->markForceSync();
        schema->updateSample(*ss);
    }
}

abciAPI void aiSchemaSync(aiSchema* schema)
{
    if (schema)
        schema->waitAsync();
}

abciAPI bool aiSchemaIsConstant(aiSchema * schema)
{
    return schema ? schema->isConstant() : false;
}

abciAPI bool aiSchemaIsDataUpdated(aiSchema* schema)
{
    return schema ? schema->isDataUpdated() : false;
}

abciAPI void aiSchemaMarkForceUpdate(aiSchema * schema)
{
    if (schema)
        schema->markForceUpdate();
}

abciAPI int aiSchemaGetNumProperties(aiSchema* schema)
{
    return schema->getNumProperties();
}

abciAPI aiProperty* aiSchemaGetPropertyByIndex(aiSchema* schema, int i)
{
    return schema->getPropertyByIndex(i);
}

abciAPI aiProperty* aiSchemaGetPropertyByName(aiSchema* schema, const char *name)
{
    return schema->getPropertyByName(name);
}

abciAPI void aiSampleSync(aiSample * sample)
{
    if (sample)
        sample->waitAsync();
}



abciAPI void aiXformGetData(aiXformSample* sample, aiXformData *dst)
{
    if (sample)
    {
        sample->getData(*dst);
    }
}

abciAPI void aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* dst)
{
    if (schema)
        *dst = schema->getSummary();
}

abciAPI void aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* dst)
{
    if (sample)
        sample->getSummary(*dst);
}

abciAPI void aiPolyMeshGetSplitSummaries(aiPolyMeshSample* sample, aiMeshSplitSummary *dst)
{
    if (sample)
        sample->getSplitSummaries(dst);
}

abciAPI void aiPolyMeshGetSubmeshSummaries(aiPolyMeshSample* sample, aiSubmeshSummary* dst)
{
    if (sample)
        sample->getSubmeshSummaries(dst);
}

abciAPI void aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, aiPolyMeshData* vbs, aiSubmeshData* ibs)
{
    if (sample)
        sample->fillVertexBuffer(vbs, ibs);
}

abciAPI void aiCameraGetData(aiCameraSample* sample, aiCameraData *dst)
{
    if (sample)
    {
        sample->getData(*dst);
    }
}

abciAPI void aiPointsGetSummary(aiPoints *schema, aiPointsSummary *dst)
{
    if (schema)
        *dst = schema->getSummary();
}
abciAPI void aiPointsSetSort(aiPoints* schema, bool v)
{
    if (schema)
        schema->setSort(v);
}
abciAPI void aiPointsSetSortBasePosition(aiPoints* schema, abcV3 v)
{
    if (schema)
        schema->setSortPosition(v);
}

abciAPI void aiPointsGetSampleSummary(aiPointsSample * sample, aiPointsSampleSummary * dst)
{
    if (sample)
        sample->getSummary(*dst);
}

abciAPI void aiPointsFillData(aiPointsSample* sample, aiPointsData *dst)
{
    if (sample)
        sample->fillData(*dst);
}


abciAPI aiPropertyType aiPropertyGetType(aiProperty* prop)
{
    return prop->getPropertyType();
}

abciAPI int aiPropertyGetTimeSamplingIndex(aiProperty* prop)
{
    return prop->getTimeSamplingIndex();
}

abciAPI const char* aiPropertyGetName(aiProperty* prop)
{
    return prop->getName().c_str();
}

abciAPI void aiPropertyGetDataPointer(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *data)
{
    prop->getDataPointer(*ss, *data);
}
abciAPI void aiPropertyCopyData(aiProperty* prop, const abcSampleSelector *ss, aiPropertyData *dst)
{
    prop->copyData(*ss, *dst);
}