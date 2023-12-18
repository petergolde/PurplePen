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
using System.Text;
using System.Globalization;

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

        // Order two angles in start/finish, with finish in range 0..360 and start in the range
        // -180-360 and before finish. Assumed that gap in <=180 degrees.
        // Returns true if the two angles with swapped in order.
        public static bool OrderGapAngles(ref float angle1, ref float angle2)
        {
            bool swapped = false;

            // Put angles into range -180 to 180
            angle1 = (float)Math.IEEERemainder(angle1, 360);
            angle2 = (float)Math.IEEERemainder(angle2, 360);
            if (Math.Abs(angle2 - angle1) > 180) {
                if (angle1 < angle2)
                    angle1 += 360;
                else
                    angle2 += 360;
            }

            if (angle1 > angle2) {
                swapped = true;
                float t = angle2;
                angle2 = angle1;
                angle1 = t;
            }

            if (angle2 < 0) {
                angle1 += 360; angle2 += 360;
            }

            return swapped;
        }


        // Encode a gap array as text. Format is 45-180,181-185,...
        public static string EncodeGaps(CircleGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return "";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < gaps.Length; ++i) {
                if (i != 0)
                    builder.Append(",");
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:R}:{1:R}", gaps[i].startAngle, gaps[i].stopAngle);
            }
            return builder.ToString();
        }

        // Decode a gap array as text. Must be the exact kind produced from EncodeGaps.
        public static CircleGap[] DecodeGaps(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            List<CircleGap> gapList = new List<CircleGap>();
            foreach (string gapText in text.Split(',')) {
                string[] splitGapText = gapText.Split(':');
                if (splitGapText.Length == 2) {
                    float start, stop;
                    if (float.TryParse(splitGapText[0], NumberStyles.Any, CultureInfo.InvariantCulture, out start) &&
                        float.TryParse(splitGapText[1], NumberStyles.Any, CultureInfo.InvariantCulture, out stop)) {
                        gapList.Add(new CircleGap(start, stop));
                    }
                }
            }

            return gapList.ToArray();
        }

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

        // Return start and stop points of the gaps, as used by MapModel.
        public static float[] StartsAndStops(CircleGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return null;

            float[] arcs = new float[gaps.Length * 2];
            for (int i = 0; i < gaps.Length; i += 1) {
                arcs[i * 2] = gaps[i].startAngle;
                arcs[i * 2 + 1] = gaps[i].stopAngle;
            }
            return arcs;
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
                arcs[i * 2] = -startArc;
                arcs[i * 2 + 1] = -((endArc - startArc + 360.0F) % 360.0F);
            }
            return arcs;
        }

        // Move a gap start/stop point to a new location. Return the new gap array. The gap array is NOT simplified.
        public static CircleGap[] MoveStartStopPoint(PointF center, float radius, CircleGap[] gaps, PointF oldPt, PointF newPt)
        {
            CircleGap[] newGaps = (CircleGap[])gaps.Clone();

            if (newPt != center) {
                for (int i = 0; i < newGaps.Length; ++i) {
                    PointF startPt = Geometry.MoveDistance(center, radius, gaps[i].startAngle);
                    PointF stopPt = Geometry.MoveDistance(center, radius, gaps[i].stopAngle);
                    float newStart, newStop;

                    if (Geometry.Distance(startPt, oldPt) < 0.01) {
                        // Moving start point of the gap.
                        newStart = Geometry.Angle(center, newPt);
                        newStop = gaps[i].stopAngle;
                    }
                    else if (Geometry.Distance(stopPt, oldPt) < 0.01) {
                        // Moving end point of the gap.
                        newStart = gaps[i].startAngle;
                        newStop = Geometry.Angle(center, newPt);
                    }
                    else {
                        continue;
                    }

                    if (OrderGapAngles(ref newStart, ref newStop)) {
                        float t = newStart; newStart = newStop; newStop = t;
                    }
                    newGaps[i] = new CircleGap(newStart, newStop);
                    break;
                }
            }

            return newGaps;
        }

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

        // Add a gap defined by two points. The gap is assumed to be <=180 degrees.
        public static CircleGap[] AddGap(PointF center, CircleGap[] original, PointF pt1, PointF pt2)
        {
            if (center == pt1 || center == pt2)
                return original;

            float angle1 = Geometry.Angle(center, pt1);
            float angle2 = Geometry.Angle(center, pt2);
            CircleGap.OrderGapAngles(ref angle1, ref angle2);
            return AddGap(original, angle1, angle2);
        }

        // Remove a gap (if any) from an array of gaps.
        public static CircleGap[] RemoveGap(CircleGap[] original, float angle)
        {
            angle = angle % 360F;
            if (angle < 0)
                angle += 360F;

            if (original == null)
                return null;

            List<CircleGap> newgaps = new List<CircleGap>(SimplifyGaps(original));
            for (int i = 0; i < newgaps.Count; ++i) {
                if ((newgaps[i].startAngle <= angle && newgaps[i].stopAngle >= angle) ||
                    (newgaps[i].startAngle <= angle-360F && newgaps[i].stopAngle >= angle-360F))
                    newgaps.RemoveAt(i);
            }

            if (newgaps.Count == 0)
                return null;
            else
                return newgaps.ToArray();
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

        // Put the gaps into a old-style bit field approximating these gaps.
        public static uint ComputeApproximateOldStyleGaps(CircleGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return 0xFFFFFFFF;              // no gaps

            uint bits = 0xFFFFFFFF;     // Start with all circle on.
            foreach (CircleGap gap in gaps) {
                // Go through each bit and determine if on or off.  A bit inefficient, but simple
                // to get right.
                for (int bitNum = 0; bitNum < 32; ++bitNum) {
                    float angle = (bitNum + 0.5F) * 360F / 32F;  // center point of gap represented by this bit
                    if ((angle >= gap.startAngle && angle <= gap.stopAngle) ||
                        ((angle - 360F) >= gap.startAngle && (angle - 360F) <= gap.stopAngle)) 
                    {
                        // Clear bit.
                        bits = Util.SetBit(bits, bitNum, false);
                    }
                }
            }

            return bits;
        } 
        
        // Rotate circle gaps by adding the rotation amount to all gaps.
        public static CircleGap[] RotateGaps(CircleGap[] original, float rotation)
        {   
            if (original == null)
                return null;    

            CircleGap[] newGaps = new CircleGap[original.Length];
            for (int i = 0; i < original.Length; ++i) {
                newGaps[i] = new CircleGap(original[i].startAngle + rotation, original[i].stopAngle + rotation);
            }

            return SimplifyGaps(newGaps);
        }
    }
}
