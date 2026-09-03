/* zpipe.c: example of proper use of zlib's inflate() and deflate()
Not copyrighted -- provided to the public domain
Version 1.4  11 December 2005  Mark Adler */

/* Version history:
1.0  30 Oct 2004  First version
1.1   8 Nov 2004  Add void casting for unused return values
Use switch statement for inflate() return values
1.2   9 Nov 2004  Add assertions to document zlib guarantees
1.3   6 Apr 2005  Remove incorrect assertion in inf()
1.4  11 Dec 2005  Add hack to avoid MSDOS end-of-line conversions
Avoid some compiler warnings for input and output buffers
*/

#include <stdio.h>
#include <string.h>
#include <assert.h>
#include "zlib.h"
#include "CommonMPQ.h"
#define CHUNK 16384

int unzip_progress = 0;

int inf(FILE *source, FILE *dest)
{
	unzip_progress = 0;
	int ret;
	unsigned have;
	z_stream strm;
	unsigned char in[CHUNK];
	unsigned char out[CHUNK];

	/* allocate inflate state */
	strm.zalloc = Z_NULL;
	strm.zfree = Z_NULL;
	strm.opaque = Z_NULL;
	strm.avail_in = 0;
	strm.next_in = Z_NULL;
	ret = inflateInit(&strm);
	if (ret != Z_OK)
		return ret;

	/* decompress until deflate stream ends or end of file */
	do {
		strm.avail_in = fread(in, 1, CHUNK, source);
		if (ferror(source)) {
			(void)inflateEnd(&strm);
			return Z_ERRNO;
		}
		if (strm.avail_in == 0)
			break;
		strm.next_in = in;

		/* run inflate() on input until output buffer not full */
		do {
			strm.avail_out = CHUNK;
			strm.next_out = out;
			ret = inflate(&strm, Z_NO_FLUSH);
			assert(ret != Z_STREAM_ERROR);  /* state not clobbered */
			switch (ret) {
			case Z_NEED_DICT:
				ret = Z_DATA_ERROR;     /* and fall through */
			case Z_DATA_ERROR:
			case Z_MEM_ERROR:
				(void)inflateEnd(&strm);
				return ret;
			}
			have = CHUNK - strm.avail_out;
			if (fwrite(out, 1, have, dest) != have || ferror(dest)) {
				(void)inflateEnd(&strm);
				return Z_ERRNO;
			}
			unzip_progress += have;
		} while (strm.avail_out == 0);

		/* done when inflate() says it's done */
	} while (ret != Z_STREAM_END);

	/* clean up and return */
	(void)inflateEnd(&strm);
	return ret == Z_STREAM_END ? Z_OK : Z_DATA_ERROR;
}

int buff_read(unsigned char* dst, unsigned char* src, int s_offset, int s_end, int count)
{
	int c = s_end - s_offset;
	if (c == 0) { return 0; }
	if (c < count){ count = c; }
	memcpy(dst, src + s_offset, count);
	return c;
}

int buff_write(unsigned char* dst, int d_offset, int d_end, unsigned char* src, int count)
{
	int c = d_end - d_offset;
	if (c == 0) { return 0; }
	if (c < count){ count = c; }
	memcpy(dst + d_offset, src, count);
	return c;
}

int inf_mem(unsigned char* src, int s_start, int s_end, unsigned char* dst, int dst_start, int dst_end)
{
	int ret;
	unsigned have;
	z_stream strm;
	unsigned char in[CHUNK];
	unsigned char out[CHUNK];

	/* allocate inflate state */
	strm.zalloc = Z_NULL;
	strm.zfree = Z_NULL;
	strm.opaque = Z_NULL;
	strm.avail_in = 0;
	strm.next_in = Z_NULL;
	ret = inflateInit(&strm);
	if (ret != Z_OK)
		return ret;

	/* decompress until deflate stream ends or end of file */
	do {
		strm.avail_in = buff_read(in, src, s_start, s_end, CHUNK);
		s_start += strm.avail_in;
		if (strm.avail_in == 0)
			break;
		strm.next_in = in;

		/* run inflate() on input until output buffer not full */
		do {
			strm.avail_out = CHUNK;
			strm.next_out = out;
			ret = inflate(&strm, Z_NO_FLUSH);
			assert(ret != Z_STREAM_ERROR);  /* state not clobbered */
			switch (ret) {
			case Z_NEED_DICT:
				ret = Z_DATA_ERROR;     /* and fall through */
			case Z_DATA_ERROR:
			case Z_MEM_ERROR:
				(void)inflateEnd(&strm);
				return ret;
			}
			have = CHUNK - strm.avail_out;

			dst_start += buff_write(dst, dst_start, dst_end, out, have);
			
		} while (strm.avail_out == 0);

		/* done when inflate() says it's done */
	} while (ret != Z_STREAM_END);

	/* clean up and return */
	(void)inflateEnd(&strm);
	return ret == Z_STREAM_END ? Z_OK : Z_DATA_ERROR;
}

bool _Decompress_z(const char * src_file, const char * dst_file)
{
	FILE *srcF = fopen(src_file, "rb");
	FILE *dstF = fopen(dst_file, "wb");
	if (NULL == srcF || NULL == dstF)
	{
		if (srcF) fclose(srcF);
		if (dstF) fclose(dstF);
		return false;
	}
	bool ret = (inf(srcF, dstF) == Z_OK);
    if (srcF) fclose(srcF);
    if (dstF) fclose(dstF);
    return ret;
}

bool _Decompress_z_mem(unsigned char* src, int s_start, int s_end, unsigned char* dst, int dst_start, int dst_end)
{
	return inf_mem(src, s_start, s_end, dst, dst_start, dst_end) == Z_OK;
}

int _Decompress_bytes()
{
	return unzip_progress;
}
