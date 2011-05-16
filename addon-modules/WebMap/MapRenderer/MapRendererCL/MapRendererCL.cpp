// This is the main DLL file.

#include "stdafx.h"
#include <assert.h>

#include "MapRendererCL.h"

MapRendererCL::PrimitiveCL::PrimitiveCL(LLVolumeParamsCL ^vol, LLVector3CL ^pos, LLQuaternionCL ^rot, LLVector3CL ^sca, array<SimpleColorCL^>^ colorsCL, int faceNum)
{	
	LLVolumeParams volumeParams;
	volumeParams.copyParams(vol->getVolumeParams());
	LLVector3 position = pos->getVector3();
	LLQuaternion rotation = rot->getQuaternion();
	LLVector3 scale = sca->getVector3();
	SimpleColor* colors = new SimpleColor[faceNum];
	for (int i = 0; i < faceNum; i++)
	{
		colors[i].A = colorsCL[i]->GetA();
		colors[i].R = colorsCL[i]->GetR();
		colors[i].G = colorsCL[i]->GetG();
		colors[i].B = colorsCL[i]->GetB();
	}
	prim = new Primitive(volumeParams, position, rotation, scale, colors, faceNum);
}

bool MapRendererCL::MapRenderCL::mapRender(
	float minX, float minY, float minZ, float maxX, float maxY, float maxZ,			
	array<PrimitiveCL^> ^primitives, int primNum,
	int width, int height,
	System::String ^destPath)
{
	logFile("\nStart map render\n");
	if (primNum == 0)
	{
		logFile("\nFinish map render\n");
		return false;
	}
	clock_t begin, end;
	//data transition from managed to unmanaged
	begin = clock();
	Primitive *prims = new Primitive[primNum];
	for (int i = 0; i < primNum; i++)
	{
		prims[i].CopyPrimitive(*(primitives[i]->getPrimitive()));
	}
	cli::array<wchar_t,1> ^ dpArr = destPath->ToCharArray();
	const int mLength = dpArr->Length;
	char * dp = new char[mLength + 1];
	for (int x = 0; x < mLength; ++x)
	{
		dp[x] = static_cast<char>(dpArr[x]);
	}
	dp[mLength] = 0;
	end = clock();
	logFile("Time cost in data transition from managed to unmanaged: ");
	logFile(end - begin);
	logFile("\n");

	//map render
	begin = clock();
	bool result = mr->MapRender(minX, minY, minZ, maxX, maxY, maxZ, prims, primNum, width, height, dp);
	end = clock();
	logFile("Time cost in map render main function: ");
	logFile(end - begin);
	logFile("\n");

	//free memory
	begin = clock();

	delete[] prims; prims = NULL;
	delete[] dp; dp = NULL;
	end = clock();
	logFile("Time cost in memory free: ");
	logFile(end - begin);
	logFile("\n");
	
	logFile("\nFinish map render\n");
	return result;
}