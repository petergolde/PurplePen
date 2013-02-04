/* Copyright (c) 2013, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // A gap in a circle. Angle of start and stop, in degrees. Angles are counter-clockwise from X-axis in
    // cartesian (positive Y is up) coordinate system.
    public struct CircleGap
    {
        public readonly float startAngle;            // Start angle of gap, in degrees
        public readonly float stopAngle;             // stop angle of gap

        public CircleGap(float startAngle, float stopAngle)
        {
            this.startAngle = startAngle;
            this.stopAngle = stopAngle;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CircleGap))
                return false;

            return (this == ((CircleGap)obj));
        }

        public override int GetHashCode()
        {
            return startAngle.GetHashCode() ^ stopAngle.GetHashCode();
        }

        public static bool operator ==(CircleGap gap1, CircleGap gap2)
        {
            return (gap1.startAngle == gap2.startAngle && gap1.stopAngle == gap2.stopAngle);
        }

        public static bool operator !=(CircleGap gap1, CircleGap gap2)
        {
            return !(gap1 == gap2);
        }

        /*
         * Static functions that work on arrays of circle gaps.
         */

        // Get the start/stop points of the gaps. Primarily useful for finding where the handles should be.
        public static PointF[] GapStartStopPoints(PointF center, float radius, CircleGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return null;

            PointF[] pts = new PointF[gaps.Length * 2];
            for (int i = 0; i < gaps.Length; ++i) {
                pts[i * 2] = Geometry.MoveDistance(center, radius, gaps[i].startAngle);
                pts[i * 2 + 1] = Geometry.MoveDistance(center, radius, gaps[i].stopAngle);
            }

            return pts;
        }

        // Get a series of arc start angle/sweep angle pairs to draw a circle with the gaps. Gaps must be in simplified form.
        public static float[] ArcStartSweeps(CircleGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return new float[2] { 0F, 360F };

            float[] arcs = new float[gaps.Length * 2];
            for (int i = 0; i < gaps.Length; i += 1) {
                float startArc = gaps[i].stopAngle;
                float endArc = (i == gaps.Length - 1) ? gaps[0].startAngle : gaps[i + 1].startAngle;
                arcs[i * 2] = startArc;
                arcs[i * 2 + 1] = (endArc - startArc + 360.0F) % 360.0F;
            }
            return arcs;
        }

#if false
        // Move a gap start/stop point to a new location. Return the new gap array. The gap array is NOT simplified.
        public static LegGap[] MoveStartStopPoint(SymPath path, LegGap[] gaps, PointF oldPt, PointF newPt)
        {
            LegGap[] newGaps = (LegGap[])gaps.Clone();
            float newLengthAlongPath = path.LengthToPoint(newPt);

            for (int i = 0; i < newGaps.Length; ++i) {
                PointF startPt = path.PointAtLength(gaps[i].distanceFromStart);
                PointF endPt = path.PointAtLength(gaps[i].distanceFromStart + gaps[i].length);

                if (Geometry.Distance(startPt, oldPt) < 0.01) {
                    // Moving start point of the gap.
                    newGaps[i].length -= (newLengthAlongPath - newGaps[i].distanceFromStart);
                    newGaps[i].distanceFromStart = newLengthAlongPath;
                }
                else if (Geometry.Distance(endPt, oldPt) < 0.01) {
                    // Moving end point of the gap.
                    newGaps[i].length = newLengthAlongPath - gaps[i].distanceFromStart;
                }
            }

            return newGaps;
        }
#endif

        // Add a new gap an an array of gaps. The resulting array is simplified. The original array may be null.
        public static CircleGap[] AddGap(CircleGap[] original, float startAngle, float stopAngle)
        {
            CircleGap newGap;
            CircleGap[] newGaps;

            // Create the new gap.
            newGap = new CircleGap(startAngle, stopAngle);

            // Add to the old gaps.
            if (original == null)
                newGaps = new CircleGap[1] { newGap };
            else {
                newGaps = new CircleGap[original.Length + 1];
                Array.Copy(original, newGaps, original.Length);
                newGaps[original.Length] = newGap;
            }

            // Simplify
            return SimplifyGaps(newGaps);
        }

        // Simplify a gap array. Sorts in order, combines overlapping gaps, and removes gaps before and beyond end of length.
        public static CircleGap[] SimplifyGaps(CircleGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return null;

            List<CircleGap> newGaps = new List<CircleGap>();

            // Remove gaps that have start before end.
            // Simplify to end angle between 0 and 360, startAngle between -360 and end angle.
            int i;
            for (i = 0; i < gaps.Length; ++i) {
                if (gaps[i].stopAngle <= gaps[i].startAngle)
                    continue;

                float stop = gaps[i].stopAngle % 360.0F;
                if (stop < 0)
                    stop += 360.0F;
                float start = gaps[i].startAngle % 360.0F;
                if (start < 0)
                    start += 360.0F;
                if (start > stop)
                    start -= 360.0F;

                newGaps.Add(new CircleGap(start, stop));
            }

            // Sort gaps by their start angle.
            newGaps.Sort(delegate(CircleGap gap1, CircleGap gap2) {
                if (gap1.startAngle < gap2.startAngle)
                    return -1;
                else if (gap1.startAngle > gap2.startAngle)
                    return 1;
                else
                    return 0;
            });

            // Combine gaps that overlap.
            i = 0;
            while (i < newGaps.Count - 1) {
                if (newGaps[i].stopAngle >= newGaps[i + 1].startAngle) {
                    if (newGaps[i].stopAngle  < newGaps[i + 1].stopAngle)
                        newGaps[i] = new CircleGap(newGaps[i].startAngle, newGaps[i + 1].stopAngle);
                    newGaps.RemoveAt(i + 1);
                }
                else
                    ++i;
            }

            // Last and first can overlap
            while (newGaps.Count >= 2 && newGaps[0].startAngle < newGaps[newGaps.Count - 1].stopAngle - 360.0F) {
                newGaps[0] = new CircleGap(newGaps[newGaps.Count - 1].startAngle - 360.0F, newGaps[0].stopAngle);
                newGaps.RemoveAt(newGaps.Count - 1);
            }

            // And we're done.
            return (newGaps.Count > 0) ? newGaps.ToArray() : null;
        }

        public static CircleGap[] ComputeCircleGaps(uint gaps)
        {
            if (gaps == 0xFFFFFFFF)
                return null;                       // no gaps
            else if (gaps == 0)
                return new CircleGap[1] { new CircleGap(-360F, 0) };  // all gap
            else {
                int firstGap = 0;

                // Find the first gap start (a 1 to 0 transition).
                for (int i = 0; i < 32; ++i) {
                    if (!Util.GetBit(gaps, i) && Util.GetBit(gaps, i - 1)) {
                        firstGap = i;
                        break;
                    }
                }

                List<CircleGap> gapList = new List<CircleGap>();
                // Now create gaps.
                int lastGapStart = firstGap;
                for (int i = firstGap; i < firstGap + 32; ++i) {
                    if (Util.GetBit(gaps, i) && !Util.GetBit(gaps, i - 1)) {
                        // found end of gap.
                        int endGap = i;

                        gapList.Add(new CircleGap((float)(lastGapStart * 360.0 / 32), (float)(endGap * 360.0 / 32)));
                    }
                    else if (!Util.GetBit(gaps, i) && Util.GetBit(gaps, i - 1)) {
                        lastGapStart = i;
                    }
                }

                return SimplifyGaps(gapList.ToArray());
            }
        }
    }
}
