// MapRendererCL.h

#pragma once
#include "..\\MapRenderer\\mapRenderer.h"
#include "..\\MapRenderer\\logFile.h"
#include <time.h>

using namespace System;


namespace MapRendererCL {
	public ref class LLProfileParamsCL
	{
	private:
		LLProfileParams* mProfileParams;
	public:
		LLProfileParamsCL(unsigned char curve, float begin, float end, float hollow)
		{
			mProfileParams = new LLProfileParams(curve, begin, end, hollow);
		}
		LLProfileParamsCL(unsigned char curve, unsigned short begin, unsigned short end, unsigned short hollow)
		{
			mProfileParams = new LLProfileParams(curve, begin, end, hollow);
		}
		~LLProfileParamsCL()
		{
			delete mProfileParams;
			mProfileParams = NULL;
		}
		LLProfileParams getProfileParams()
		{
			return *mProfileParams;
		}
	};

	public ref class LLPathParamsCL
	{
	private:
		LLPathParams* mPathParams;	
	public:
		LLPathParamsCL(unsigned char curve, unsigned short begin, unsigned short end, unsigned char scx, unsigned char scy, unsigned char shx, unsigned char shy, char twistend, char twistbegin, char radiusoffset, char tx, char ty, unsigned char revolutions, char skew)
		{
			mPathParams = new LLPathParams(curve, begin, end, scx, scy, shx, shy, twistend, twistbegin, radiusoffset, tx, ty, revolutions, skew);
		}
		~LLPathParamsCL()	{ delete mPathParams; mPathParams = NULL; }
		LLPathParams getPathParams()
		{
			return *mPathParams;
		}
	};

	public ref class LLVector3CL
	{
	private:
		LLVector3 *mVector3;
	public:
		LLVector3CL()
		{
			mVector3 = new LLVector3();
		}
		LLVector3CL(float x, float y, float z)
		{
			mVector3 = new LLVector3(x, y, z);
		}

		~LLVector3CL()	{ delete mVector3; mVector3 = NULL; }
		LLVector3 getVector3()
		{
			return *mVector3;
		}
	};

	public ref class LLQuaternionCL
	{
	private:
		LLQuaternion *mQuaternion;
	public:
		LLQuaternionCL(): mQuaternion(new LLQuaternion())	{}
		LLQuaternionCL(float x, float y, float z, float w): mQuaternion(new LLQuaternion(x, y, z, w))	{}
		~LLQuaternionCL()	{ delete mQuaternion; mQuaternion = NULL; }
		LLQuaternion getQuaternion()
		{
			return *mQuaternion;
		}
	};

	public ref class LLUUIDCL
	{
	private:
		LLUUID* uuid;
	public:
		LLUUIDCL(System::String^ id)
		{
			cli::array<wchar_t,1> ^idArr = id->ToCharArray();
			const int nLength = idArr->Length;
			char * cp = new char[nLength + 1];
			for (int x = 0; x < nLength; ++x)
			{
				cp[x] = static_cast<char>(idArr[x]);
			}
			cp[nLength] = 0;
			uuid = new LLUUID(cp);
			delete[] cp;
			cp = NULL;
		}
		~LLUUIDCL()	{ delete uuid; uuid = NULL; }
		LLUUID getUUID()
		{
			return *uuid;
		}	
	};

	public ref class SimpleColorCL
	{
	private:
		SimpleColor* scolor;
	public:
		SimpleColorCL(byte a, byte r, byte g, byte b) : scolor(new SimpleColor(a, r, g, b)) {}
		SimpleColorCL() : scolor(new SimpleColor()) {}
		~SimpleColorCL()	{ delete scolor; scolor = NULL;}
		SimpleColor getColor()
		{
			return *scolor;
		}
		byte GetA()
		{
			return scolor->A; 
		}
		byte GetB() 
		{
			return scolor->B; 
		}
		byte GetR()
		{
			return scolor->R;
		}
		byte GetG()
		{
			return scolor->G;
		}
	};

	public ref class LLVolumeParamsCL
	{
		// TODO: Add your methods for this class here.
	private:
		LLVolumeParams* volumeParams;
	public:
		LLVolumeParamsCL(LLProfileParamsCL^ profile, LLPathParamsCL^ path)
		{
			LLProfileParams prop = profile->getProfileParams();
			LLPathParams pap = path->getPathParams();
			
			volumeParams = new LLVolumeParams(prop, pap);
		}
		~LLVolumeParamsCL()	{ delete volumeParams; volumeParams = NULL; }
		LLVolumeParams getVolumeParams()
		{
			return *volumeParams;
		}
	};

	public ref class PrimitiveCL
	{
	private:
		Primitive *prim;
	public:
		PrimitiveCL(LLVolumeParamsCL ^vol, LLVector3CL ^pos, LLQuaternionCL ^rot, LLVector3CL ^sca, array<SimpleColorCL^>^ colorsCL, int faceNum);
	
		~PrimitiveCL()	
		{
			if (prim)
			{
				delete prim;
				prim = NULL;
			}
		}
		Primitive* getPrimitive()
		{
			return prim;
		}

		bool isPrimitiveNull()
		{
			return prim == NULL;
		}

	};

	public ref class MapRenderCL
	{
	private:
		MapRenderer *mr;

	public:
		MapRenderCL(): mr(new MapRenderer())	{}
		~MapRenderCL()	{ delete mr; mr = NULL; }

		bool mapRender(
			float minX, float minY, float minZ, float maxX, float maxY, float maxZ,			
			array<PrimitiveCL^> ^primitives, int primNum,
			int width, int height,
			System::String ^destPath);
		

		MapRenderer getMapRenderer()
		{
			return *mr;
		}
		
	};
}
