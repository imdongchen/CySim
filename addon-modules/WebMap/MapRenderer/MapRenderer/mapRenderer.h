/***************************************************************************************\
* mapRenderer组件主文件，包含llcommon.lib, llprimitive.lib, llvolume.lib
* 这里还有类axis和camera，是为方便调试用的，可删
* logFile用于生成日志文件，现在还很不成熟，只记录了少量可能发生的错误，应写成可记录时间、过程的
* 真正的日志文件
\***************************************************************************************/

#pragma once
#pragma warning (disable: 4018)

#include <vector>
#include <d3dx9.h>
#include "logFile.h"
#include "../llprimitive/llprimitive.h"
#include "../llprimitive/lltextureentry.h"
#include "prim.h"
#include "windows.h"
#include "../llcommon/llmemory.h"
#include <boost/shared_ptr.hpp>

//
// determin the size of d3dbuffer, thus the size of the resulted bmp image
//
float Width = 256;
float Height = 256;
bool Windowed = true;
IDirect3DDevice9 *Device = NULL;
typedef boost::shared_ptr<Prim> Prim_ptr;
typedef std::vector<Prim_ptr> Prim_ptr_vec;

class __declspec(dllexport) Primitive
{
public:
	LLVolumeParams VolumeParams;
	LLVector3 Position;
	LLQuaternion Rotation;
	LLVector3 Scale;
	SimpleColor* FaceColors;
	int FaceNumByOpenSim;
	int FaceNumByHippo;
	
	Primitive()	{ FaceColors = NULL; }
	Primitive(LLVolumeParams vol, LLVector3 pos, LLQuaternion rot, LLVector3 sca, SimpleColor* colors, int faceNum)
	{
		VolumeParams.copyParams(vol);
		Position.set(pos);
		Rotation.set(rot);
		Scale.set(sca);
		FaceNumByOpenSim = faceNum;
		FaceColors = new SimpleColor[faceNum];
		for (int i = 0; i < faceNum; i++)
		{
			FaceColors[i].set(colors[i]);
		}
	}

	~Primitive()
	{
		if (FaceColors)
		{
			delete[] FaceColors;
			FaceColors = NULL;
		}
	}

	void CopyPrimitive(const Primitive &prim)
	{
		VolumeParams.copyParams(prim.VolumeParams);
		Position.set(prim.Position);
		Rotation.set(prim.Rotation);
		Scale.set(prim.Scale);
		FaceNumByOpenSim = prim.FaceNumByOpenSim;
		FaceColors = new SimpleColor[prim.FaceNumByOpenSim];
		for (int i = 0; i < prim.FaceNumByOpenSim; i++)
		{
			FaceColors[i].set(prim.FaceColors[i]);
		}
	}
};

class __declspec(dllexport) MapRenderer
{
public:
	bool MapRender(float minX, float minY, float minZ, float maxX, float maxY, float maxZ,
			Primitive* primitives, int primNum,
			int width, int height,
			char* destPath);

	bool render(
		float minX, float minY, float minZ, float maxX, float maxY, float maxZ,
		Prim* prims, int primNum,
		int width, int height,
		char* destPath);

	void getFullName(char* fullName, char* fileName, char *path, int type);

	bool InitD3D(
		IDirect3DDevice9 **device,
		const int width,
		const int height, 
		const bool windowed);

	int DrawBasicScene();

	bool ScreenShot (float width, float height, char* filename);
};
