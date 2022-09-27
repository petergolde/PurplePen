// GDIPlusNative.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "GDIPlusNative.h"


// This is an example of an exported function.
extern "C"   GDIPLUSNATIVE_API  void __stdcall DarkenBits(BYTE * bmFrom, int strideFrom, BYTE * bmTo, int strideTo, int height, int widthInBytes)
{
	int chunks, leftovers;

	int CPUInfo[4];
	__cpuid(CPUInfo, 1);

	if ((CPUInfo[3] & (1 << 26)) != 0) {
		chunks = widthInBytes / 16;
		leftovers = widthInBytes - (chunks * 16);
	}
	else {
		// No SSE2 support.
		chunks = 0;
		leftovers = widthInBytes;
	}

	for (int scan = 0; scan < height; ++scan) {
		BYTE* pixelFrom = bmFrom + scan * strideFrom;
		BYTE* pixelTo = bmTo + scan * strideTo;
		__m128i * chunkFrom = (__m128i *) pixelFrom;
		__m128i * chunkTo = (__m128i *) pixelTo;

		for (int i = 0; i < chunks; ++i) {
			// Must use unaligned loads and stores, because the bitmaps aren't guaranteed to be aligned.
			__m128i minResult = _mm_min_epu8(_mm_loadu_si128(chunkFrom), _mm_loadu_si128(chunkTo));
			_mm_storeu_si128(chunkTo, minResult);
			chunkFrom++;
			chunkTo++;
		}

		pixelFrom = (BYTE *)chunkFrom;
		pixelTo = (BYTE *)chunkTo;
		
		for (int i = 0; i < leftovers; ++i) {
			BYTE bFrom = *pixelFrom, bTo = *pixelTo;
			if (bFrom < bTo)
				*pixelTo = bFrom;
			pixelFrom++;
			pixelTo++;
		}
	}
}
