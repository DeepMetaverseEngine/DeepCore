using System;
using DeepCore.GUI.Display;
using DeepCore;
using DeepCore.GUI.Data;

namespace DeepCore.GUI.Cell.Game
{
    public class CSprite
    {
        public const byte CD_TYPE_MAP = 0;
        public const byte CD_TYPE_ATK = 1;
        public const byte CD_TYPE_DEF = 2;
        public const byte CD_TYPE_EXT = 3;

        private SpriteSet data;
        private CAnimates animates;
        private CCollides collides;

        private int animCount => data.AnimateCount;
        //        private int[] frameCounts;
        //
        //         //frameAnimate[animate id][frame id] = canimate frame index.
        //         private string[] AnimateNames;
        //         //
        //         private int[][] FrameAnimate;
        //         // frameAnimate[animate id][frame id] = ccollide frame index.
        //         private int[][] FrameCDMap;
        //         // frameAnimate[animate id][frame id] = ccollide frame index.
        //         private int[][] FrameCDAtk;
        //         // frameAnimate[animate id][frame id] = ccollide frame index.
        //         private int[][] FrameCDDef;
        //         // frameAnimate[animate id][frame id] = ccollide frame index.
        //         private int[][] FrameCDExt;
        // 
        //         private float[][] FrameAlpha;

        public string Name { get => data.Name; }

        public CAnimates Animates { get { return animates; } }
        public CCollides Collides { get { return collides; } }
        public SpriteSet Data { get { return data; } }
        public CPJAtlas Atlas { get { return animates.tiles; } }

        public CSprite(SpriteSet tsprite, CPJAtlas tiles)
        {
            this.data = tsprite;
            {
                //scene parts
                int scenePartCount = tsprite.Parts.Length;	// --> u16
                CAnimates animates = new CAnimates(scenePartCount, tiles);
                for (int i = 0; i < scenePartCount; i++)
                {
                    var part = tsprite.Parts[i];
                    animates.setPart(i,
                        part.PartX,
                        part.PartY,
                        part.PartTileID,
                        part.PartTileTrans,
                        part.PartAlpha,
                        part.PartRotate,
                        part.PartScaleX,
                        part.PartScaleY);
                }
                //scene frames
                animates.setFrames(tsprite.FrameParts);

                if (tsprite.AppendData.Contains("Scale="))
                {
                    //由于不能加入引号，所以不能安装xml，json标准格式解析
                    //XmlDocument doc = new XmlDocument();
                    //doc.LoadXml("<data " + tsprite.AppendData + "/>");
                    //string localstr = "<data " + tsprite.AppendData + " />";
                    //XmlDocument doc = XMLTool.Instance.GetXml(Encoding.UTF8.GetBytes(localstr));

                    //XmlElement element = doc.DocumentElement; 

                    //if (element.Name.Equals("data"))
                    //{
                    //    animates.setScale(float.Parse(element.Attributes["Scale"].Value));
                    //}

                    int startIndex = tsprite.AppendData.IndexOf("Scale=");
                    int lastIndex = tsprite.AppendData.LastIndexOf('\n');
                    float scale = Parser.ParseFloat(tsprite.AppendData.Substring(startIndex + 6, lastIndex - startIndex - 6));
                    animates.setScale(scale, scale);
                }

                //cd parts
                int cdCount = tsprite.Blocks.Length;	// --> u16
                CCollides collides = new CCollides(cdCount);
                for (int i = 0; i < cdCount; i++)
                {
                    var block = tsprite.Blocks[i];
                    collides.setCDRect(i,
                        block.BlockMask,
                        block.BlockX1,
                        block.BlockY1,
                        block.BlockW,
                        block.BlockH);
                }
                //cd frames
                collides.setFrames(tsprite.FrameBlocks);

                this.animates = animates;
                this.collides = collides;
            }
            {
                //animCount = tsprite.AnimateCount;
                //frameCounts = new int[animCount];

                //AnimateNames = tsprite.AnimateNames;

                //                 FrameAnimate = new int[animCount][];
                //                 FrameCDMap = new int[animCount][];
                //                 FrameCDAtk = new int[animCount][];
                //                 FrameCDDef = new int[animCount][];
                //                 FrameCDExt = new int[animCount][];
                //                 FrameAlpha = new float[animCount][];
                //                 for (int a = 0; a < animCount; a++)
                //                 {
                //                     int fc = tsprite.Animates[a].Frames.Length;
                //                     //frameCounts[a] = fc;
                // //                     FrameAnimate[a] = new int[fc];
                // //                     FrameCDMap[a] = new int[fc];
                // //                     FrameCDAtk[a] = new int[fc];
                // //                     FrameCDDef[a] = new int[fc];
                // //                     FrameCDExt[a] = new int[fc];
                // //                     FrameAlpha[a] = new float[fc];
                // //                     for (int f = 0; f < fc; f++)
                // //                     {
                // //                         FrameAnimate[a][f] = tsprite.FrameAnimate[a][f];
                // //                         FrameCDMap[a][f] = tsprite.FrameCDMap[a][f];
                // //                         FrameCDAtk[a][f] = tsprite.FrameCDAtk[a][f];
                // //                         FrameCDDef[a][f] = tsprite.FrameCDDef[a][f];
                // //                         FrameCDExt[a][f] = tsprite.FrameCDExt[a][f];
                // //                         FrameAlpha[a][f] = tsprite.FrameAlpha[a][f];
                // //                     }
                //                 }
            }
        }

        //	------------------------------------------------------------------------------------------


        public int getFrameImageCount(int anim, int frame = 0)
        {
            //return animates.getComboFrameCount(FrameAnimate[anim][frame]);
            return animates.getComboFrameCount(data.Animates[anim].Frames[frame].FramePartIndex);
        }

        public int getFrameTileID(int anim, int frame = 0, int sub = 0)
        {
            //return animates.getFrameTileID(FrameAnimate[anim][frame], sub);
            return animates.getFrameTileID(data.Animates[anim].Frames[frame].FramePartIndex, sub);
        }

        //         public float getFrameImageX(int anim, int frame = 0, int sub = 0)
        //         {
        //             return animates.getFrameX(FrameAnimate[anim][frame], sub);
        //         }
        // 
        //         public float getFrameImageY(int anim, int frame = 0, int sub = 0)
        //         {
        //             return animates.getFrameY(FrameAnimate[anim][frame], sub);
        //         }
        // 
        //         public float getFrameImageWidth(int anim, int frame = 0, int sub = 0)
        //         {
        //             return animates.getFrameW(FrameAnimate[anim][frame], sub);
        //         }
        // 
        //         public float getFrameImageHeight(int anim, int frame = 0, int sub = 0)
        //         {
        //             return animates.getFrameH(FrameAnimate[anim][frame], sub);
        //         }
        // 
        //         public Trans getFrameImageTransform(int anim, int frame = 0, int sub = 0)
        //         {
        //             return animates.getFrameTransform(FrameAnimate[anim][frame], sub);
        //         }
        // 
        //         public int getAvaliableTileID()
        //         {
        //             for (int anim = 0; anim < getAnimateCount(); anim++)
        //             {
        //                 for (int frame = 0; frame < getFrameCount(anim); frame++)
        //                 {
        //                     for (int sub = 0; sub < getFrameImageCount(anim, frame); sub++)
        //                     {
        //                         int tid = getFrameTileID(anim, frame, sub);
        //                         if (!Atlas.IsNullTile(tid))
        //                         {
        //                             return tid;
        //                         }
        //                     }
        //                 }
        //             }
        //             return 0;
        //         }

        //	------------------------------------------------------------------------------------------
        // 
        //         public int[][] getCDAnimates(byte type)
        //         {
        //             switch (type)
        //             {
        //                 case CD_TYPE_MAP:
        //                     return FrameCDMap;
        //                 case CD_TYPE_ATK:
        //                     return FrameCDAtk;
        //                 case CD_TYPE_DEF:
        //                     return FrameCDDef;
        //                 case CD_TYPE_EXT:
        //                     return FrameCDExt;
        //             }
        //             return null;
        //         }
        // 
        //         public int getFrameCDCount(int anim, int frame, byte type)
        //         {
        //             int[][] out_animates = getCDAnimates(type);
        //             if (out_animates != null)
        //             {
        //                 return collides.getComboFrameCount(out_animates[anim][frame]);
        //             }
        //             return -1;
        //         }
        // 
        //         public CCD getFrameCD(int anim, int frame, byte type, int sub)
        //         {
        //             int[][] out_animates = getCDAnimates(type);
        //             if (out_animates != null)
        //             {
        //                 return collides.getFrameCD(out_animates[anim][frame], sub);
        //             }
        // 
        //             return null;
        //         }

        //	------------------------------------------------------------------------------------------

        public CCD getVisibleBounds()
        {
            return animates.getAllBounds();
        }

        //         public CCD getVisibleBounds(int anim, int frame)
        //         {
        //             if (anim < animCount)
        //             {
        //                 if (frame < frameCounts[anim])
        //                 {
        //                     CCD out_bounds = new CCD();
        //                     out_bounds.X1 = float.MaxValue;
        //                     out_bounds.X2 = float.MinValue;
        //                     out_bounds.Y1 = float.MaxValue;
        //                     out_bounds.Y2 = float.MinValue;
        //                     int frameid = FrameAnimate[anim][frame];
        //                     int count = animates.getComboFrameCount(frameid);
        //                     for (int i = 0; i < count; i++)
        //                     {
        //                         out_bounds.X1 = Math.Min(out_bounds.X1, animates.getFrameX(frameid, i));
        //                         out_bounds.Y1 = Math.Min(out_bounds.Y1, animates.getFrameY(frameid, i));
        //                         out_bounds.X2 = Math.Max(out_bounds.X2, animates.getFrameX(frameid, i) + animates.getFrameW(frameid, i));
        //                         out_bounds.Y2 = Math.Max(out_bounds.Y2, animates.getFrameY(frameid, i) + animates.getFrameH(frameid, i));
        //                     }
        //                     return out_bounds;
        //                 }
        //             }
        // 
        //             return null;
        //         }

        public CCD getVisibleBounds(int anim)
        {
            if (anim < animCount)
            {
                CCD out_bounds = new CCD();
                out_bounds.X1 = float.MaxValue;
                out_bounds.X2 = float.MinValue;
                out_bounds.Y1 = float.MaxValue;
                out_bounds.Y2 = float.MinValue;
                for (int f = getFrameCount(anim) - 1; f >= 0; --f)
                {
                    //int frameid = FrameAnimate[anim][f];
                    int frameid = data.Animates[anim].Frames[f].FramePartIndex;
                    int count = animates.getComboFrameCount(frameid);
                    for (int i = 0; i < count; i++)
                    {
                        out_bounds.X1 = Math.Min(out_bounds.X1, animates.getFrameX(frameid, i));
                        out_bounds.Y1 = Math.Min(out_bounds.Y1, animates.getFrameY(frameid, i));
                        out_bounds.X2 = Math.Max(out_bounds.X2, animates.getFrameX(frameid, i) + animates.getFrameW(frameid, i));
                        out_bounds.Y2 = Math.Max(out_bounds.Y2, animates.getFrameY(frameid, i) + animates.getFrameH(frameid, i));
                    }
                }
                return out_bounds;
            }
            return null;
        }



        public CCD getCDBounds()
        {
            return collides.getAllBounds();
        }

        public int getAnimateIndex(string animate_name)
        {
            for (int i = animCount - 1; i >= 0; --i)
            {
                if (string.Equals(data.Animates[i].Name, animate_name))
                {
                    return i;
                }
            }
            return -1;
        }

        public string getAnimateName(int anim)
        {
            return data.Animates[anim].Name;
        }


        public int getAnimateCount()
        {
            return animCount;
        }

        public int getFrameCount(int anim)
        {
            //return frameCounts[anim];
            return data.Animates[anim].Frames.Length;
        }

        public void render(Graphics g, int anim, int frame, float dx = 0, float dy = 0)
        {
            if ((anim < animCount) && (frame < getFrameCount(anim)))
            {
                var tframe = data.Animates[anim].Frames[frame];
                float olda = g.CurrentAlpha;
                g.AddAlpha(tframe.FrameAlpha);
                animates.render(g, tframe.FramePartIndex, dx, dy);
                g.SetAlpha(olda);
            }
        }

        public void addVertex(VertexBuffer v, int anim, int frame, float dx, float dy)
        {
            if ((anim < animCount) && (frame < getFrameCount(anim)))
            {
                var tframe = data.Animates[anim].Frames[frame];
                animates.addVertex(v, tframe.FramePartIndex, dx, dy);
            }
        }

        public void beginImage(Graphics g)
        {
            animates.beginImage(g);
        }
    }

    public struct TSpriteMetaAnimateFrame
    {
        public CSprite sprite;
        public int anim, frame;
    }

    /// <summary>
    /// CPJSprite 控制类
    /// </summary>
    public class CSpriteController : ICellSpriteController
    {
        private readonly CSprite mMeta;
        private int mCurAnimate = 0;
        private int mCurFrame = 0;
        private int mFramPassMS = 0;
        private int mFrameMS = 0;
        private int mPlayTimes = -1;
        private int mCurrentPlayTimes = 0;
        public bool IsAutoPlay = true;


        public CSpriteController(CSprite src)
        {
            mMeta = src;
            if (src.Data.FPS > 0)
            {
                mFrameMS = 1000 / src.Data.FPS;
            }
        }
        public bool Update(int deltaMS)
        {
            if (IsAutoPlay)
            {
                if (mMeta.Data.FPS > 0)
                {
                    mFramPassMS += deltaMS;
                }
                NextCycFrame();
                PlayTimesTick();
                return true;
            }
            return false;
        }
        public void Render(Graphics graphics, float dx = 0, float dy = 0)
        {
            mMeta.render(graphics, this.CurrentAnimate, this.CurrentFrame, dx, dy);
        }

        public void Dispose()
        {
            IsAutoPlay = false;
            m_FinishCallback = null;
        }


        public CSprite Meta
        {
            get { return mMeta; }
        }
        public int CurrentFrame
        {
            get { return mCurFrame; }
            private set
            {
                if (mFrameMS > 0)
                {
                    if (mFramPassMS > mFrameMS || mCurFrame >= value)
                    {
                        mCurFrame = value;
                        mFramPassMS = 0;
                    }

                }
                else
                {
                    mCurFrame = value;
                }
            }
        }
        public int CurrentAnimate
        {
            get { return mCurAnimate; }
        }
        public bool IsEndFrame
        {
            get
            {
                if (mCurFrame + 1 >= mMeta.getFrameCount(mCurAnimate))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void SetCurrentAnimate(string anim_name)
        {
            int anim = mMeta.getAnimateIndex(anim_name);
            if (anim >= 0)
            {
                SetCurrentAnimate(anim);
            }
        }

        public void SetCurrentAnimate(int anim)
        {
            mCurAnimate = anim;
            mCurAnimate = CMath.CycNum(mCurAnimate, 0, mMeta.getAnimateCount());
            mCurFrame = CMath.CycNum(mCurFrame, 0, mMeta.getFrameCount(mCurAnimate));
        }

        public void SetCurrentFrame(int anim, int index)
        {
            mCurAnimate = CMath.CycNum(anim, 0, mMeta.getAnimateCount());
            mCurFrame = CMath.CycNum(index, 0, mMeta.getFrameCount(mCurAnimate));
        }


        public bool NextFrame()
        {
            CurrentFrame++;
            int acount = mMeta.getFrameCount(mCurAnimate);
            if (mCurFrame >= acount)
            {
                mCurFrame = acount - 1;
                return true;
            }
            else
            {
                return false;
            }
        }


        public void NextCycFrame()
        {
            CurrentFrame++;
            int acount = mMeta.getFrameCount(mCurAnimate);
            if (acount > 0)
            {
                mCurFrame %= acount;
            }

        }

        public void NextCycFrame(int restart)
        {
            CurrentFrame++;
            int acount = mMeta.getFrameCount(mCurAnimate);
            if (mCurFrame < acount)
            {
            }
            else
            {
                if (acount > 0)
                {
                    mCurFrame = (restart % acount);
                }
            }
        }

        public bool PrewFrame()
        {
            CurrentFrame--;
            if (mCurFrame < 0)
            {
                mCurFrame = 0;
                return true;
            }
            else
            {
                return false;
            }
        }


        public void PrewCycFrame()
        {
            CurrentFrame--;
            if (mCurFrame < 0)
            {
                mCurFrame = mMeta.getFrameCount(mCurAnimate) - 1;
            }
        }

        public void PrewCycFrame(int restart)
        {
            CurrentFrame--;
            if (mCurFrame < 0)
            {
                mCurFrame = (restart % mMeta.getFrameCount(mCurAnimate));
            }
        }



        /// <summary>
        /// 播放动画:anim动画名、times次数(-1=无限).
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="times"></param>
        /// <param name="callBack"></param>
        public void PlayAnimate(int anim, int times, CSpriteEventHandler callBack)
        {
            mPlayTimes = times;
            m_FinishCallback = callBack;
            SetCurrentAnimate(anim);
            mCurFrame = 0;
            IsAutoPlay = true;
        }

        /// <summary>
        /// 播放动画:anim_name动画名、times次数(-1=无限).
        /// </summary>
        /// <param name="anim_name"></param>
        /// <param name="times"></param>
        /// <param name="callBack"></param>
        public void PlayAnimate(string anim_name, int times, CSpriteEventHandler callBack)
        {
            int anim = mMeta.getAnimateIndex(anim_name);
            if (anim >= 0)
            {
                PlayAnimate(anim, times, callBack);
            }
        }

        public void StopAnimate(bool needCallBack)
        {
            IsAutoPlay = false;
            if (m_FinishCallback != null && needCallBack)
            {
                m_FinishCallback(this);
            }
            m_FinishCallback = null;
        }

        private void PlayTimesTick()
        {
            if (mPlayTimes < 0) { return; }
            if (!IsEndFrame) { return; }

            mCurrentPlayTimes++;

            if (mCurrentPlayTimes >= mPlayTimes)
            {
                IsAutoPlay = false;
                if (m_FinishCallback != null)
                {
                    CSpriteEventHandler temp = m_FinishCallback;
                    m_FinishCallback = null;
                    temp(this);
                    temp = null;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------

        private CSpriteEventHandler m_FinishCallback;
        public event CSpriteEventHandler Finish { add { m_FinishCallback += value; } remove { m_FinishCallback -= value; } }

        //---------------------------------------------------------------------------------------------------

    }

    /// <summary>
    /// 动画编辑器控制类
    /// </summary>
    public interface ICellSpriteController
    {
        int CurrentFrame { get; }
        int CurrentAnimate { get; }
        bool IsEndFrame { get; }

        void SetCurrentAnimate(string anim);
        void SetCurrentAnimate(int anim);
        void SetCurrentFrame(int anim, int index);

        bool NextFrame();
        void NextCycFrame();
        void NextCycFrame(int restart);

        bool PrewFrame();
        void PrewCycFrame();
        void PrewCycFrame(int restart);
    }

    public delegate void CSpriteEventHandler(ICellSpriteController sender);

}
