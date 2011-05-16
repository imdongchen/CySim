#include "prim.h"
#include "../llcommon/lluuid.h"


PrimMesh::PrimMesh(IDirect3DDevice9 *device, LLVolumeFace *volumeFace, SimpleColor scolor)
{
	initialize(device, volumeFace, scolor);
}
void PrimMesh::initialize(IDirect3DDevice9 *device, LLVolumeFace *volumeFace, SimpleColor scolor)
{
	primMesh = NULL;
	int numVertices = volumeFace->mVertices.size();
	int numIndices = volumeFace->mIndices.size();
	HRESULT hr = D3DXCreateMeshFVF(
							numIndices/3, //NumFaces，三角形个数
							numVertices, //NumVertices
							D3DXMESH_MANAGED, //Options
							FVF_VERTEX, //FVF
							device, 
							&primMesh); //产出的mesh结果

	//向空mesh填充点坐标
	Vertex* v = 0;
	primMesh->LockVertexBuffer(0, (void**)&v);

	for(int j = 0; j<numVertices; j++)
	{
		float p0 = volumeFace->mVertices[j].mPosition.mV[0];
		float p1 = volumeFace->mVertices[j].mPosition.mV[1];
		float p2 = volumeFace->mVertices[j].mPosition.mV[2];
		float n0 = volumeFace->mVertices[j].mNormal.mV[0];
		float n1 = volumeFace->mVertices[j].mNormal.mV[1];
		float n2 = volumeFace->mVertices[j].mNormal.mV[2];

		v[j] = Vertex(p0, p1, p2, n0, n1, n2, scolor);
	}
	primMesh->UnlockVertexBuffer();

	//填充index
	WORD* i = 0;
	primMesh->LockIndexBuffer(0, (void**)&i);
	for(U32 j=0; j<numIndices; j++)
	{
		i[j] = volumeFace->mIndices[j];
	}
	primMesh->UnlockIndexBuffer();

	//指明哪些三角形属于哪个Subset，为贴纹理作准备，这里将所有三角形都归一个类了
	DWORD* attributeBuffer = 0;
	primMesh->LockAttributeBuffer(0, &attributeBuffer);
	for(int j=0; j<numIndices/3; j++) 
	{
		attributeBuffer[j] = 0;
	}
	primMesh->UnlockAttributeBuffer();	
}

Prim::Prim(IDirect3DDevice9* device, 
		   LLVolumeFace* volumeFace, 
		   LLVector3 &pos, 
		   LLQuaternion &rot,
		   LLVector3 &scale,
		   SimpleColor &scolor)
			: PrimMesh(device, volumeFace, scolor)
{
	mPosition = pos;
	mRotation = rot;
	mScale = scale;
	LLMatrix4 llMat;
	getLocalMat4(llMat);
	LLMat2D3DMat(llMat, d3dMat);	
}

void Prim::initialize(IDirect3DDevice9* device, 
		   LLVolumeFace* volumeFace, 
		   LLVector3 &pos, 
		   LLQuaternion &rot,
		   LLVector3 &scale,
		   SimpleColor &scolor)
{
	PrimMesh::initialize(device, volumeFace, scolor);
	mPosition = pos;
	mRotation = rot;
	mScale = scale;
	LLMatrix4 llMat;
	getLocalMat4(llMat);
	LLMat2D3DMat(llMat, d3dMat);	
}




bool Prim::draw(IDirect3DDevice9* device)
{
	device->SetTransform(D3DTS_WORLD, &d3dMat);
	device->SetMaterial(&WHITE_MTRL);
	
	if(FAILED(primMesh->DrawSubset(0)))
	{
		logFile("drawing prim failed");
		return false;
	}
	return true;
}


void Prim::LLMat2D3DMat(LLMatrix4 &llmat, D3DXMATRIX &d3dmat)
{
	for(int i=0; i<4; i++)
		for(int j=0; j<4; j++)
			d3dmat(i, j) = llmat.mMatrix[i][j];
}