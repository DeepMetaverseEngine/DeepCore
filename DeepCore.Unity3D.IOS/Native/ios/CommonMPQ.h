#ifndef _MF_COMMON_MPQ_H_
#define _MF_COMMON_MPQ_H_

#include <stdio.h>

extern "C"
{
	// [DllImport("__Internal")]
	// public static extern bool _Decompress_z(string srcFile, string dstFile);
	bool _Decompress_z(const char * src_file, const char * dst_file);

	//[DllImport("__Internal")]
	//public static extern bool _Decompress_z_mem(byte[] src, int s_offset, int s_size, byte[] dst, int dst_offset, int dst_size);
	bool _Decompress_z_mem(unsigned char* src, int s_start, int s_end, unsigned char* dst, int dst_start, int dst_end);

	int _Decompress_bytes();
}

#endif /* _MF_COMMON_MPQ_H_ */