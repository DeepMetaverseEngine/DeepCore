//
//  CommonUnityLib.m
//  CommonUnityLib
//
//  Created by zyf on 14-3-27.
//  Copyright (c) 2014年 zyf. All rights reserved.
//

//#import "CommonUnityLib.h"

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <QuartzCore/QuartzCore.h>
#import <OpenGLES/ES2/gl.h>

#include "CommonMPQ.h"
#include "MFMD5.h"

extern "C"
{
    bool _SysFontTest(const char * pText,
                      const char * fontName,
                      int fontStyle,
                      int fontSize,
				      int bgCount,
                      int expectSizeW,
                      int expectSizeH,
                      int& outW,
                      int& outH);
    
    bool _SysFontTexture2(const char * pText,
                          const char * fontName,
                          int fontStyle,
                          int fontSize,
                          int fontColorRGBA,
                          int bgCount,
                          int bgColorRGBA,
                          int expectSizeW,
                          int expectSizeH,
                          int glTextureID);

    bool _SysFontTexture2_Ptr(const char * pText,
                          const char * fontName,
                          int fontStyle,
                          int fontSize,
                          int fontColorRGBA,
                          int bgCount,
                          int bgColorRGBA,
                          int expectSizeW,
                          int expectSizeH,
                          void* glTextureID_Ptr);
    
    
    bool _SysFontGetPixels(const char * pText,
                           const char * fontName,
                           int fontStyle,
                           int fontSize,
                           int fontColorRGBA,
                           int bgCount,
                           int bgColorRGBA,
                           int pixelW,
                           int pixelH,
                           unsigned char * pixels);

    bool _SysFontGetPixels_Color32(const char * pText,
                           const char * fontName,
                           int fontStyle,
                           int fontSize,
                           int fontColorRGBA,
                           int bgCount,
                           int bgColorRGBA,
                           int pixelW,
                           int pixelH,
                           void * pixels);
    
    
}


typedef enum FontStyle
{
    STYLE_PLAIN = 0,
    STYLE_BOLD = 1,
    STYLE_ITALIC = 2,
    STYLE_UNDERLINED = 4,
}
kFontStyle;

int _nextPoT(int size)
{
    size = size - 1;
    size = size | (size >> 1);
    size = size | (size >> 2);
    size = size | (size >> 4);
    size = size | (size >> 8);
    size = size | (size >>16);
    return size + 1;
}

CGSize _calculateStringSizeWithFontOrZFont(NSString *str, id font)
{
    CGSize dim = CGSizeZero;
    dim = [str sizeWithFont:font
          constrainedToSize:CGSizeMake(65535, 65535)
              lineBreakMode:NSLineBreakByWordWrapping];
    dim.width  = ceilf(dim.width);
    dim.height = ceilf(dim.height);
    return dim;
}

id _SysGetFont(const char * fontName, int fontSize, int fontStyle)
{
    NSString * fntName = [NSString stringWithUTF8String:fontName];
    // create the font
    id font = [UIFont fontWithName:fntName size:fontSize];
    if (!font)
    {
        font = [UIFont fontWithName:@"Helvetica-Bold" size:fontSize];
        if (! font)
        {
            font = [UIFont systemFontOfSize:fontSize];
        }
    }
     return font;
}

bool _SysFontTest(const char * pText,
                  const char * fontName,
                  int fontStyle,
                  int fontSize,
                  int bgCount,
                  int expectSizeW,
                  int expectSizeH,
                  int& outW,
                  int& outH)
{
    outW = 0;
    outH = 0;
    NSString * str  = [NSString stringWithUTF8String:pText];
    CGSize dim;
    
    // create the font
    id font = _SysGetFont(fontName, fontSize, fontStyle);
    if (font)
    {
        dim = _calculateStringSizeWithFontOrZFont(str, font);
    }
    else
    {
        return false;
    }
    
    // adjust text rect
    if (expectSizeW > 0 && expectSizeW > dim.width)
    {
        dim.width = expectSizeW;
    }
    if (expectSizeH > 0 && expectSizeH > dim.height)
    {
        dim.height = expectSizeH;
    }
    dim.width += 3;
    dim.height += 3;
    outW = (int)ceilf(dim.width);
    outH = (int)ceilf(dim.height);
    
    return true;
}

#define		 BORDER_Null        0
#define      BORDER_Shadow      1
#define      BORDER_Border_4    4
#define      BORDER_Border      8
#define      BORDER_Shadow_L_T  10
#define      BORDER_Shadow_C_T  11
#define      BORDER_Shadow_R_T  12
#define      BORDER_Shadow_L_C  13
#define      BORDER_Shadow_C_C  14
#define      BORDER_Shadow_R_C  15
#define      BORDER_Shadow_L_B  16
#define      BORDER_Shadow_C_B  17
#define      BORDER_Shadow_R_B  18

void _SysDrawInContext(CGContextRef context,
                       id font,
                       NSString* str,
                       CGSize dim,
                       int fontStyle,
                       int fontColorRGBA,
                       int bgCount,
                       int bgColorRGBA)
{
    //CGContextSetRGBFillColor(context, 0.5f, 0.5f, 0.5f, 1.0f);
    //CGContextFillRect(context, CGRectMake(0, 0, dim.width, dim.height));
    CGContextSetShouldSmoothFonts(context, true);
    
	float fr=(float)((fontColorRGBA>>24)&0xff)/255.0f;
    float fg=(float)((fontColorRGBA>>16)&0xff)/255.0f;
    float fb=(float)((fontColorRGBA>>8 )&0xff)/255.0f;
    float fa=(float)((fontColorRGBA>>0 )&0xff)/255.0f;
    
    if (STYLE_UNDERLINED == fontStyle)
    {
        //draw underline
        CGContextSetRGBStrokeColor(context, fr, fg, fb, fa);  // set as the text's color
        CGContextSetLineWidth(context, 2.0f);
        
        CGPoint leftPoint  = CGPointMake(1,dim.height - 2);
        CGPoint rightPoint = CGPointMake(dim.width - 1,dim.height - 2);
        
        CGContextMoveToPoint(context, leftPoint.x, leftPoint.y);
        CGContextAddLineToPoint(context, rightPoint.x, rightPoint.y);
        CGContextStrokePath(context);
    }
    
    // measure text size with specified font and determine the rectangle to draw text in
    NSTextAlignment align = NSTextAlignmentLeft;
    if (bgCount != 0)
    {
        const char offset_8[8][2]={
            { 0, 0},{ 1, 0},{ 2, 0},
            { 0, 1},/*1, 1*/{ 2, 1},
            { 0, 2},{ 1, 2},{ 2, 2}};
        const char offset_4[8][2]={
            /*0, 0*/{ 1, 0},/*2, 0*/
            { 0, 1},/*1, 1*/{ 2, 1},
            /*0, 2*/{ 1, 2},/*2, 2*/};
        float br=(float)((bgColorRGBA>>24)&0xff)/255.0f;
        float bg=(float)((bgColorRGBA>>16)&0xff)/255.0f;
        float bb=(float)((bgColorRGBA>>8 )&0xff)/255.0f;
        float ba=(float)((bgColorRGBA>>0 )&0xff)/255.0f;
        CGContextSetRGBFillColor(context, br, bg, bb, ba);
        
		switch(bgCount)
		{
			case BORDER_Border:
				for (int i=0; i < 8; i++)
				{
					[str drawInRect:CGRectMake(offset_8[i][0], offset_8[i][1], dim.width, dim.height)
						   withFont:font
					  lineBreakMode:NSLineBreakByWordWrapping
						  alignment:align];
				}
				break;   
			case BORDER_Border_4:
				for (int i=0; i < 4; i++)
				{
					[str drawInRect:CGRectMake(offset_4[i][0], offset_4[i][1], dim.width, dim.height)
						   withFont:font
					  lineBreakMode:NSLineBreakByWordWrapping
						  alignment:align];
				}  
				break; 
			case BORDER_Shadow:
				[str drawInRect:CGRectMake(1, 2, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
				break; 

            case BORDER_Shadow_L_T:
                [str drawInRect:CGRectMake(0, 0, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;
            case BORDER_Shadow_C_T:
                [str drawInRect:CGRectMake(1, 0, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;
            case BORDER_Shadow_R_T:
                [str drawInRect:CGRectMake(2, 0, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;

            case BORDER_Shadow_L_C:
                [str drawInRect:CGRectMake(0, 1, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;
            case BORDER_Shadow_R_C:
                [str drawInRect:CGRectMake(2, 1, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;

            case BORDER_Shadow_L_B:
                [str drawInRect:CGRectMake(0, 2, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;
            case BORDER_Shadow_C_B:
                [str drawInRect:CGRectMake(1, 2, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;
            case BORDER_Shadow_R_B:
                [str drawInRect:CGRectMake(2, 2, dim.width, dim.height)
                       withFont:font
                  lineBreakMode:NSLineBreakByWordWrapping
                      alignment:align];
                break;
		}
    }
    
    
    CGContextSetRGBFillColor(context, fr, fg, fb, fa);
    [str drawInRect:CGRectMake(1, 1, dim.width, dim.height)
           withFont:font
      lineBreakMode:NSLineBreakByWordWrapping
          alignment:align];
	[str drawInRect:CGRectMake(1, 1, dim.width, dim.height)
           withFont:font
      lineBreakMode:NSLineBreakByWordWrapping
          alignment:align];
}




bool _SysFontTexture2           (const char * pText,
                                 const char * fontName,
                                 int fontStyle,
                                 int fontSize,
                                 int fontColorRGBA,
                                 int bgCount,
                                 int bgColorRGBA,
                                 int expectSizeW,
                                 int expectSizeH,
                                 int glTextureID)
{
    bool bRet = false;
    do
    {
        NSString * str  = [NSString stringWithUTF8String:pText];
       
        CGSize dim = CGSizeMake(expectSizeW, expectSizeH);
        
        id font = _SysGetFont(fontName, fontSize, fontStyle);
        if (!font)
        {
            break;
        }
        
        int _potw = (int)ceilf(dim.width);  //_nextPoT(dim.width);//
        int _poth = (int)ceilf(dim.height); //_nextPoT(dim.height);//
        
        unsigned char* data = new unsigned char[(_potw * _poth * 4)];
        memset(data, 0, ( _potw * _poth * 4));
        
        // draw text
        CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
        //浮点数*4再强转成int 不一定为4的倍数，此时调试ios7会产生崩溃//
        CGContextRef context = CGBitmapContextCreate(data,
                                                     _potw, _poth,
                                                     8, (_potw * 4),
                                                     colorSpace,
                                                     kCGImageAlphaPremultipliedLast | kCGBitmapByteOrder32Big);
        CGColorSpaceRelease(colorSpace);
        if (!context)
        {
            delete[] data;
            break;
        }
        UIGraphicsPushContext(context);
        {
            //CGContextTranslateCTM(context, 0.0f, startH + 1.5f);
            //CGContextScaleCTM(context, 1.0f, 1.0f);
            _SysDrawInContext(context, font, str, dim, fontStyle,fontColorRGBA, bgCount, bgColorRGBA);
        }
        UIGraphicsPopContext();
        CGContextRelease(context);
        {
			GLenum err = GL_NO_ERROR;
            //Update GL Texture
            glBindTexture(GL_TEXTURE_2D, glTextureID);
            glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, _potw, _poth, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
			while((err = glGetError()) != GL_NO_ERROR)
			{
				printf("GLError: 0x%x\n", err);
			}
		}
        delete[] data;
        
        bRet = true;
    }
    while (0);
    
    return bRet;
}

bool _SysFontTexture2_Ptr       (const char * pText,
                                 const char * fontName,
                                 int fontStyle,
                                 int fontSize,
                                 int fontColorRGBA,
                                 int bgCount,
                                 int bgColorRGBA,
                                 int expectSizeW,
                                 int expectSizeH,
                                 void* glTextureID_Ptr)
{
	int glTextureID = (int)((long)glTextureID_Ptr);
	return _SysFontTexture2(pText,fontName,fontStyle,fontSize,fontColorRGBA,bgCount,bgColorRGBA,expectSizeW,expectSizeH,glTextureID);
}

bool _SysFontGetPixels(const char * pText,
                       const char * fontName,
                       int fontStyle,
                       int fontSize,
                       int fontColorRGBA,
                       int bgCount,
                       int bgColorRGBA,
                       int pixelW,
                       int pixelH,
                       unsigned char * pixels)
{
    bool bRet = false;
    do
    {
        NSString * str  = [NSString stringWithUTF8String:pText];
        
        CGSize dim = CGSizeMake(pixelW, pixelH);
        
        id font = _SysGetFont(fontName, fontSize, fontStyle);
        if (!font)
        {
            break;
        }
        
        int _potw = pixelW;
        int _poth = pixelH;
        
        memset(pixels, 0, ( _potw * _poth * 4));
        
        // draw text
        CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
        //浮点数*4再强转成int 不一定为4的倍数，此时调试ios7会产生崩溃//
        CGContextRef context = CGBitmapContextCreate(pixels,
                                                     _potw, _poth,
                                                     8, (_potw * 4),
                                                     colorSpace,
                                                     kCGImageAlphaPremultipliedLast | kCGBitmapByteOrder32Big);
        CGColorSpaceRelease(colorSpace);
        
        if (! context)
        {
            break;
        }
        UIGraphicsPushContext(context);
        {
            _SysDrawInContext(context, font, str, dim, fontStyle,fontColorRGBA, bgCount, bgColorRGBA);
        }
        UIGraphicsPopContext();
        
        CGContextRelease(context);
        
        bRet = true;
    }
    while (0);
    
    return bRet;
    
    
}

bool _SysFontGetPixels_Color32(const char * pText,
	const char * fontName,
	int fontStyle,
	int fontSize,
	int fontColorRGBA,
	int bgCount,
	int bgColorRGBA,
	int pixelW,
	int pixelH,
	void * pixels32)
{
	unsigned char * pixels = (unsigned char *)pixels32;
	return _SysFontGetPixels(pText,fontName,fontStyle,fontSize,fontColorRGBA,bgCount,bgColorRGBA,pixelW,pixelH,pixels);
}
