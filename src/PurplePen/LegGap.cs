/* Copyright (c) 2006-2008, Peter Golde
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

namespace PurplePen
{
    // A gap in a leg. Location along leg from start, and length of the gap.
    public struct LegGap
    {
        public float distanceFromStart;            // Distance along leg to start of the gap.
        public float length;                               // length of the gap

        public LegGap(float distanceFromStart, float length)
        {
            this.distanceFromStart = distanceFromStart;
            this.length = length;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LegGap))
                return false;

            return (this == ((LegGap)obj));
        }

        public override int GetHashCode()
        {
            return distanceFromStart.GetHashCode() ^ length.GetHashCode();
        }

        public static bool operator ==(LegGap gap1, LegGap gap2)
        {
            return (gap1.distanceFromStart == gap2.distanceFromStart && gap1.length == gap2.length);
        }

        public static bool operator !=(LegGap gap1, LegGap gap2)
        {
            return !(gap1 == gap2);
        }

        /*
         * Static functions that work on arrays of leg gaps.
         */

        // Get the start/stop points of the gaps. Primarily useful for finding where the handles should be.
        public static PointF[] GapStartStopPoints(SymPath path, LegGap[] gaps)
        {
            if (gaps == null || gaps.Length == 0)
                return null;

            PointF[] pts = new PointF[gaps.Length * 2];
            for (int i = 0; i < gaps.Length; ++i) {
                pts[i * 2] = path.PointAtLength(gaps[i].distanceFromStart);
                pts[i * 2 + 1] = path.PointAtLength(gaps[i].distanceFromStart + gaps[i].length);
            }

            return pts;
        }

        // Move a gap start/stop point to a new location. Return the new gap array. The gap array is NOT simplified.
        public static LegGap[] MoveStartStopPoint(SymPath path, LegGap[] gaps, PointF oldPt, PointF newPt)
        {
            LegGap[] newGaps = (LegGap[]) gaps.Clone();
            float newLengthAlongPath = path.LengthToPoint(newPt);

            for (int i = 0; i < newGaps.Length; ++i) {
                PointF startPt = path.PointAtLength(gaps[i].distanceFromStart);
                PointF endPt = path.PointAtLength(gaps[i].distanceFromStart + gaps[i].length);

                if (Util.Distance(startPt, oldPt) < 0.01) {
                    // Moving start point of the gap.
                    newGaps[i].length -= (newLengthAlongPath - newGaps[i].distanceFromStart);
                    newGaps[i].distanceFromStart = newLengthAlongPath;
                }
                else if (Util.Distance(endPt, oldPt) < 0.01) {
                    // Moving end point of the gap.
                    newGaps[i].length = newLengthAlongPath - gaps[i].distanceFromStart;
                }
            }

            return newGaps;
        }

        // Add a new gap an an array of gaps. The resulting array is simplified. The original array may be null.
        public static LegGap[] AddGap(SymPath path, LegGap[] original, PointF pt1, PointF pt2)
        {
            float dist1, dist2;
            LegGap newGap;
            LegGap[] newGaps;

            // map points to closes points on the line.
            path.DistanceFromPoint(pt1, out pt1);
            path.DistanceFromPoint(pt2, out pt2);

            // Map to distances along the line.
            dist1 = path.LengthToPoint(pt1);
            dist2 = path.LengthToPoint(pt2);

            // Create the new gap.
            if (dist1 < dist2)
                newGap = new LegGap(dist1, dist2 - dist1);
            else 
                newGap = new LegGap(dist2, dist1 - dist2);

            // Add to the old gaps.
            if (original == null)
                newGaps = new LegGap[1] { newGap };
            else {
                newGaps = new LegGap[original.Length + 1];
                Array.Copy(original, newGaps, original.Length);
                newGaps[original.Length] = newGap;
            }

            // Simplify
            return SimplifyGaps(newGaps, path.Length);
        }

        // Simplify a gap array. Sorts in order, combines overlapping gaps, and removes gaps before and beyond end of length.
        public static LegGap[] SimplifyGaps(LegGap[] gaps, float pathLength)
        {
            if (gaps == null || gaps.Length == 0)
                return null;

            List<LegGap> newGaps = new List<LegGap>(gaps);

            // Truncating to the length of the path. Doesn't remove gaps, but may set their length to negative, which
            // will cause them to be removed later.
            int i;
            for (i = 0; i < newGaps.Count; ++i) {
                if (newGaps[i].distanceFromStart + newGaps[i].length > pathLength) {
                    newGaps[i] = new LegGap(newGaps[i].distanceFromStart, pathLength - newGaps[i].distanceFromStart);
                }

                if (newGaps[i].distanceFromStart < 0) {
                    newGaps[i] = new LegGap(0, newGaps[i].length + newGaps[i].distanceFromStart);
                }
            }

            // Remove any gaps with 0 or negative length.
            i = 0;
            while (i < newGaps.Count) {
                if (newGaps[i].length <= 0)
                    newGaps.RemoveAt(i);
                else
                    ++i;
            }

            // Sort gaps by their start point.
            newGaps.Sort(delegate(LegGap gap1, LegGap gap2) {
                if (gap1.distanceFromStart < gap2.distanceFromStart)
                    return -1;
                else if (gap1.distanceFromStart > gap2.distanceFromStart)
                    return 1;
                else
                    return 0;
            });

            // Combine gaps that overlap.
            i = 0;
            while (i < newGaps.Count - 1) {
                if (newGaps[i].distanceFromStart + newGaps[i].length >= newGaps[i + 1].distanceFromStart) {
                    if (newGaps[i].distanceFromStart + newGaps[i].length < newGaps[i + 1].distanceFromStart + newGaps[i + 1].length)
                        newGaps[i] = new LegGap(newGaps[i].distanceFromStart, newGaps[i + 1].length + (newGaps[i + 1].distanceFromStart - newGaps[i].distanceFromStart));
                    newGaps.RemoveAt(i + 1);
                }
                else
                    ++i;
            }

            // And we're done.
            return (newGaps.Count > 0) ? newGaps.ToArray() : null;
        }


        // Split a path into multiple paths based on an array of LegGaps.  The gaps might extend beyond the end of the 
        // or the beginning of the path. The gaps array need not be in simplified form.
        public static SymPath[] SplitPathWithGaps(SymPath pathInitial, LegGap[] gaps)
        {
            // Get the length of the path.
            float pathLength = pathInitial.Length;

            // Simply and sort the gaps.
            gaps = SimplifyGaps(gaps, pathLength);

            // If no gaps length, the entire path is correct.
            if (gaps == null)
                return new SymPath[1] { pathInitial };

            // Transform into start/stop distances from beginning of path.
            float[] starts = new float[gaps.Length + 1];
            float[] ends = new float[gaps.Length + 1];
            starts[0] = 0;
            ends[gaps.Length] = pathLength;
            for (int i = 0; i < gaps.Length; ++i) {
                ends[i] = gaps[i].distanceFromStart;
                starts[i + 1] = gaps[i].distanceFromStart + gaps[i].length;
            }

            // Each 2 points is a new path.
            List<SymPath> list = new List<SymPath>(starts.Length);
            for (int i = 0; i < starts.Length; ++i) {
                SymPath p = pathInitial.Segment(pathInitial.PointAtLength(starts[i]), pathInitial.PointAtLength(ends[i]));
                if (p != null)
                    list.Add(p);
            }

            return list.ToArray();
        }

        // Transform a gap array to a new path, keeping close gaps in the closest position on the new path. Remove far away gaps.
        public static LegGap[] MoveGapsToNewPath(LegGap[] oldGaps, SymPath oldPath, SymPath newPath)
        {
            oldGaps = SimplifyGaps(oldGaps, oldPath.Length);
            if (oldGaps == null)
                return null;

            PointF oldStart, oldEnd, newStart, newEnd;  // ends points of the gaps
            float distanceStart, distanceEnd;  // distance between oldStart and newStart, distance between oldEnd and newEnd.
            List<LegGap> newGaps = new List<LegGap>();

            // Move gap to a new gap by converting start and end to point, finding closest points on new path.
            // If the gap has moved too far, just remove it, else transformit.
            for (int i = 0; i < oldGaps.Length; ++i) {
                oldStart = oldPath.PointAtLength(oldGaps[i].distanceFromStart);
                oldEnd = oldPath.PointAtLength(oldGaps[i].distanceFromStart + oldGaps[i].length);
                distanceStart = newPath.DistanceFromPoint(oldStart, out newStart);
                distanceEnd = newPath.DistanceFromPoint(oldEnd, out newEnd);

                // If the new gap is close enough to the old gap, then add the new gap.
                if (distanceStart + distanceEnd <= 2 * oldGaps[i].length) {
                    float newDistanceToStart = newPath.LengthToPoint(newStart);
                    float newDistanceToEnd = newPath.LengthToPoint(newEnd);
                    if (newDistanceToStart < newDistanceToEnd)
                        newGaps.Add(new LegGap(newDistanceToStart, newDistanceToEnd - newDistanceToStart));
                    else
                        newGaps.Add(new LegGap(newDistanceToEnd, newDistanceToStart - newDistanceToEnd));
                }
            }

            // Simply the new gap array.
            return SimplifyGaps(newGaps.ToArray(), newPath.Length);
        }

        
    }
}
