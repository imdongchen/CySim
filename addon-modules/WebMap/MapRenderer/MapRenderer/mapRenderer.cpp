#include "mapRenderer.h"
#include "renderElements.h"
#include <time.h>
const int BMP = 0;
const int JPG = 1;

bool MapRenderer::MapRender(
		float minX, float minY, float minZ, float maxX, float maxY, float maxZ,
		Primitive* primitives, 
		int primNum,
		int width, int height,
		char* destPath)
{
	if (primNum == 0)
		return false;

	clock_t begin, end;

	begin = clock();
	InitD3D(&Device, width, height, Windowed);
	Prim* prims = new Prim[primNum * 6];

	logFile("primitive number: ");
	logFile(primNum);
	logFile("\n");

	int primNumber = 0;
	for (int i = 0; i < primNum; i++)
	{
		LLVolume* llvolume = new LLVolume(primitives[i].VolumeParams, 4.0f, false, false);
		int faceNumByHippo = llvolume->getNumFaces();
		primitives[i].FaceNumByHippo = faceNumByHippo;
		int faceNumByOpenSim = primitives[i].FaceNumByOpenSim;
		int faceNum = faceNumByHippo < faceNumByOpenSim ? faceNumByHippo : faceNumByOpenSim;
		for (int j = 0; j < faceNum; j++)
		{
			LLVolumeFace llvolumeFace(llvolume->getVolumeFace(j));	
			prims[primNumber + j].initialize(Device, 
				&llvolumeFace, 
				primitives[i].Position, primitives[i].Rotation, primitives[i].Scale,
				primitives[i].FaceColors[j]);
		}
		primNumber += faceNum;
		llvolume->destroy();
		llvolume = NULL;
	}
	end = clock();
	logFile("Time cost in mesh produce and store: ");
	logFile(end - begin);
	logFile("\n");
	logFile("total prims: ");
	logFile(primNumber); logFile("\n");
	
	begin = clock();
	bool res = render(
		minX, minY, minZ, maxX, maxY, maxZ, 
		prims, primNumber,
		width, height,
		destPath);

	delete[] prims; prims = NULL;
	Device->Release();
	end = clock();
	logFile("Time cost in rendering all faces: ");
	logFile(end - begin); logFile("\n");

	return res;
}


void MapRenderer::getFullName(char* fullName, char* fileName, char *path, int type)
{
	//最好加些语句，判断cachePath是否以"\\"结尾，若否，则加上"\\"
	strcpy(fullName, path);
	strcat(fullName, fileName);
	if( type == 0)
		strcat(fullName, ".bmp");
	else
		strcat(fullName, ".jpg");
}



bool MapRenderer::InitD3D(IDirect3DDevice9** device, const int width, const int height,	const bool windowed)
{
	HRESULT hr = 0;

	IDirect3D9* d3d9 = NULL;
	d3d9 = Direct3DCreate9(D3D_SDK_VERSION);

	if(!d3d9)
	{
		logFile("Create D3D failed");
		return 0;
	}

	D3DCAPS9 caps;
	d3d9->GetDeviceCaps(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, &caps);

	int vp = 0;
	if( caps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT )
		vp = D3DCREATE_HARDWARE_VERTEXPROCESSING;
	else
		vp = D3DCREATE_SOFTWARE_VERTEXPROCESSING;

	// Step 3: Fill out the D3DPRESENT_PARAMETERS structure.
 
	D3DPRESENT_PARAMETERS d3dpp;
	d3dpp.BackBufferWidth            = width;
	d3dpp.BackBufferHeight           = height;
	d3dpp.BackBufferFormat           = D3DFMT_A8R8G8B8;
	d3dpp.BackBufferCount            = 1;
	d3dpp.MultiSampleType            = D3DMULTISAMPLE_NONE;
	d3dpp.MultiSampleQuality         = 0;
	d3dpp.SwapEffect                 = D3DSWAPEFFECT_DISCARD; 
	d3dpp.hDeviceWindow              = 0;//hWnd; //hWnd
	d3dpp.Windowed                   = windowed;
	d3dpp.EnableAutoDepthStencil     = true; 
	d3dpp.AutoDepthStencilFormat     = D3DFMT_D24S8;
	d3dpp.Flags                      = 0;
	d3dpp.FullScreen_RefreshRateInHz = D3DPRESENT_RATE_DEFAULT;
	d3dpp.PresentationInterval       = D3DPRESENT_INTERVAL_IMMEDIATE;

	// Step 4: Create the device.

	hr = d3d9->CreateDevice(
		D3DADAPTER_DEFAULT, // primary adapter
		D3DDEVTYPE_HAL,         // device type
		0,//hWnd,               // window associated with device, hWnd
		vp,                 // vertex processing
	    &d3dpp,             // present parameters
	    device);            // return created device

	if( FAILED(hr) )
	{
		// try again using a 16-bit depth buffer
		d3dpp.AutoDepthStencilFormat = D3DFMT_D16;
		
		hr = d3d9->CreateDevice(
			D3DADAPTER_DEFAULT,
			D3DDEVTYPE_HAL,
			0,//hWnd
			vp,
			&d3dpp,
			device);

		if( FAILED(hr) )
		{
			d3d9->Release(); // done with d3d9 ObjectShape
			logFile("create d3d device failed");
			return false;
		}
	}

	d3d9->Release();

	return true;
}



bool MapRenderer::render(
			float minX, float minY, float minZ, float maxX, float maxY, float maxZ,
			Prim* prims, int primNum, 
			int width, int height,
			char* destPath)
{
	//正射投影
	D3DXMATRIX proj;
	D3DXMatrixOrthoRH(&proj, 
		maxX - minX, //width of the view volume, TO BE CHANEGED!!
		maxY - minY, //height of the view volume   TO BE CHANGED!!
		0, //z-near
		maxZ - minZ); //z-far

	Device->SetTransform(D3DTS_PROJECTION, &proj);

	float viewX = (minX+maxX)/2;
	float viewY = (minY+maxY)/2;
	D3DXVECTOR3 position(viewX, viewY, maxZ);
	D3DXVECTOR3 target(viewX, viewY, minZ);
	D3DXVECTOR3 up(0.0f, 1.0f, 0.0f);
    D3DXMATRIX V;
	D3DXMatrixLookAtRH(&V, &position, &target, &up);
	Device->SetTransform(D3DTS_VIEW, &V);

	Device->Clear(0, 0, D3DCLEAR_TARGET | D3DCLEAR_ZBUFFER, 0x00000000, 1.0f, 0);
	Device->BeginScene();
	DrawBasicScene();
	
	for (int i = 0; i < primNum; i++)
	{
		bool res = prims[i].draw(Device);
		if (res == false)
		{
			logFile("mesh draw error");
			return false;
		}
	}

	Device->EndScene();

	if(!ScreenShot(width, height, destPath))
	{
		logFile("screenShot Error");
		return false;
	}

	Device->Present(0, 0, 0, 0);
	return true;
}

int MapRenderer::DrawBasicScene()
{
	Device->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_LINEAR);
	Device->SetSamplerState(0, D3DSAMP_MINFILTER, D3DTEXF_LINEAR);
	Device->SetSamplerState(0, D3DSAMP_MIPFILTER, D3DTEXF_POINT);

	Device->SetRenderState(D3DRS_LIGHTING, false);
	Device->SetRenderState(D3DRS_NORMALIZENORMALS, true);
	Device->SetRenderState(D3DRS_FILLMODE, D3DFILL_SOLID);
	return 0;
}

bool MapRenderer::ScreenShot (float width, float height, char* filename)
{
	IDirect3DSurface9	*tmp = NULL;
	IDirect3DSurface9	*back = NULL;

	//生成固定颜色模式的离屏表面（Width和 Height为屏幕或窗口的宽高）
	if (FAILED (Device->CreateOffscreenPlainSurface(width, height, D3DFMT_A8R8G8B8, D3DPOOL_DEFAULT, &tmp, NULL)))
		return false;

	// 获得BackBuffer的D3D Surface
	if (FAILED (Device->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &back)))
		return false;

	// Copy一下，，需要时转换颜色格式
	if (FAILED (D3DXLoadSurfaceFromSurface(tmp, NULL, NULL, back, NULL, NULL, D3DX_FILTER_NONE, 0)))
		return false;

	// 保存成BMP格式
	if (FAILED (D3DXSaveSurfaceToFileA(filename, D3DXIFF_BMP, tmp, NULL, NULL)))
		return false;

	// 释放Surface，防止内存泄漏
	tmp->Release();
	back->Release();
	return true;
}

