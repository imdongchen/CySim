#pragma once

#include <d3dx9.h>

struct SimpleColor
{
public:
	byte A, R, G, B;
	SimpleColor(byte a, byte r, byte g, byte b)
	{
		A = a; R = r; G = g; B = b;
	}
	SimpleColor()
	{
	}
	void set(SimpleColor &sc)
	{
		A = sc.A;
		R = sc.R;
		G = sc.G;
		B = sc.B;
	}
};

struct Vertex
{
	//Vertex(){}
	Vertex(float x, float y, float z)
	{
		_x  = x;  _y  = y;  _z  = z;
	}
	Vertex(
		float x, float y, float z,
		float nx, float ny, float nz,
		SimpleColor color)
	{
		_x  = x;  _y  = y;  _z  = z;
		_nx = nx; _ny = ny; _nz = nz;
		_color = D3DCOLOR_ARGB(color.A, color.R, color.G, color.B);
	}
	float _x, _y, _z;
    float _nx, _ny, _nz;
	D3DCOLOR _color;
};

#define FVF_VERTEX (D3DFVF_XYZ | D3DFVF_NORMAL | D3DFVF_DIFFUSE)


struct ColorVertex
{
	ColorVertex(float x, float y, float z, D3DCOLOR color)
	{
		_x = x; _y = y; _z = z; 
		_color = color;
		
	}
	float _x, _y, _z;
	D3DCOLOR _color;
};
#define FVF_COLORVERTEX (D3DFVF_XYZ | D3DFVF_DIFFUSE)