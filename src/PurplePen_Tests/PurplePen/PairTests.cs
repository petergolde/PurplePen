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

#if TEST
//******************************
// Written by Peter Golde
// Copyright (c) 2004-2005, Wintellect
//
// Use and restribution of this code is subject to the license agreement 
// contained in the file "License.txt" accompanying this file.
//******************************

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
	/// <summary>
	/// Tests for the Pair struct.
	/// </summary>
	[TestClass]
	public class PairTests
	{
        /// <summary>
        /// Class that doesn't implement any IComparable.
        /// </summary>
        class Unorderable
        {
            public override bool Equals(object obj)
            {
                return obj is Unorderable;
            }

            public override int GetHashCode()
            {
                return 42;
            }
        }

        /// <summary>
        /// Comparable that compares ints, sorting odds before evens.
        /// </summary>
        class OddEvenComparable : System.IComparable
        {
            public readonly int val;

            public OddEvenComparable(int v)
            {
                val = v;
            }

            public int CompareTo(object other)
            {
                int e1 = val;
                int e2 = ((OddEvenComparable)other).val;
                if ((e1 & 1) == 1 && (e2 & 1) == 0)
                    return -1;
                else if ((e1 & 1) == 0 && (e2 & 1) == 1)
                    return 1;
                else if (e1 < e2)
                    return -1;
                else if (e1 > e2)
                    return 1;
                else
                    return 0;
            }

            public override bool Equals(object obj)
            {
                if (obj is OddEvenComparable)
                    return CompareTo((OddEvenComparable)obj) == 0;
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return val.GetHashCode();
            }

        }

        /// <summary>
        /// Comparable that compares ints, sorting odds before evens.
        /// </summary>
        class GOddEvenComparable : System.IComparable<GOddEvenComparable>
        {
            public readonly int val;

            public GOddEvenComparable(int v)
            {
                val = v;
            }

            public int CompareTo(GOddEvenComparable other)
            {
                int e1 = val;
                int e2 = other.val;
                if ((e1 & 1) == 1 && (e2 & 1) == 0)
                    return -1;
                else if ((e1 & 1) == 0 && (e2 & 1) == 1)
                    return 1;
                else if (e1 < e2)
                    return -1;
                else if (e1 > e2)
                    return 1;
                else
                    return 0;
            }

            public override bool Equals(object obj)
            {
                return (obj is GOddEvenComparable) && CompareTo((GOddEvenComparable)obj) == 0;
            }

            public override int GetHashCode()
            {
                return val.GetHashCode();
            }
        }

        /// <summary>
        /// Test basic Pair creation.  
		/// </summary>
		[TestMethod]
		public void Creation()
		{
			Pair<int,double> p1 = new Pair<int,double>();

			Assert.AreEqual(0, p1.First);
			Assert.AreEqual(0.0, p1.Second);

			Pair<int,string> p2 = new Pair<int,string>(42, "hello");

			Assert.AreEqual(42, p2.First);
			Assert.AreEqual("hello", p2.Second);

			Pair<string,object> p3 = new Pair<string,object>();

			Assert.IsNull(p3.First);
			Assert.IsNull(p3.Second);

			object o = new object();
			Pair<Pair<string,int>,object> p4 = new Pair<Pair<string,int>,object>(new Pair<string,int>("foo", 12), o);
			Pair<string,int> p5 = p4.First;

			Assert.AreEqual("foo", p5.First);
			Assert.AreEqual(12, p5.Second);
			Assert.AreSame(o, p4.Second);
		}


		[TestMethod]
		public void Equals()
		{
			Pair<int,string> p1 = new Pair<int,string>(42, new string('z', 3));
			Pair<int,string> p2 = new Pair<int,string>(53, new string('z', 3));
			Pair<int,string> p3 = new Pair<int,string>(42, new string('z', 4));
			Pair<int,string> p4 = new Pair<int,string>(42, new string('z', 3));
			Pair<int,string> p5 = new Pair<int,string>(122, new string('y', 3));
			Pair<int,string> p6 = new Pair<int,string>(122, null);
			Pair<int,string> p7 = new Pair<int,string>(122, null);
			bool f;

			f = p1.Equals(p2);		Assert.IsFalse(f);
			f = p1.Equals(p3);		Assert.IsFalse(f);
			f = p1.Equals(p4);		Assert.IsTrue(f);
			f = p1.Equals(p5);		Assert.IsFalse(f);
			f = p1.Equals("hi");		Assert.IsFalse(f);
			f = p6.Equals(p7);		Assert.IsTrue(f);
			f = p1 == p2;		Assert.IsFalse(f);
			f = p1 == p3;		Assert.IsFalse(f);
			f = p1 == p4;		Assert.IsTrue(f);
			f = p1 == p5;		Assert.IsFalse(f);
			f = p6 == p7;		Assert.IsTrue(f);
			f = p1 != p2;		Assert.IsTrue(f);
			f = p1 != p3;		Assert.IsTrue(f);
			f = p1 != p4;		Assert.IsFalse(f);
			f = p1 != p5;		Assert.IsTrue(f);
			f = p6 != p7;		Assert.IsFalse(f);
		}

		[TestMethod]
		public void HashCode()
		{
			Pair<int,string> p1 = new Pair<int,string>(42, new string('z', 3));
			Pair<int,string> p2 = new Pair<int,string>(53, new string('z', 3));
			Pair<int,string> p3 = new Pair<int,string>(42, new string('z', 4));
			Pair<int,string> p4 = new Pair<int,string>(42, new string('z', 3));
			Pair<int,string> p5 = new Pair<int,string>(122, new string('y', 3));
			Pair<int,string> p6 = new Pair<int,string>(122, null);
			Pair<int,string> p7 = new Pair<int,string>(122, null);

			int h1 = p1.GetHashCode();
			int h2 = p2.GetHashCode();
			int h3 = p3.GetHashCode();
			int h4 = p4.GetHashCode();
			int h5 = p5.GetHashCode();
			int h6 = p6.GetHashCode();
			int h7 = p7.GetHashCode();

			bool f;
			f = h1 == h2;		Assert.IsFalse(f);
			f = h1 == h3;		Assert.IsFalse(f);
			f = h1 == h4;		Assert.IsTrue(f);
			f = h1 == h5;		Assert.IsFalse(f);
			f = h6 == h7;		Assert.IsTrue(f);
		}

		[TestMethod]
		public void Stringize()
		{
			Pair<int,string> p1 = new Pair<int,string>(42, new string('z', 3));
			Pair<int,string> p2 = new Pair<int,string>(0, "hello");
			Pair<int,string> p3 = new Pair<int,string>(-122, null);
			Pair<string,int> p4 = new Pair<string,int>(null, 11);

			Assert.AreEqual("First: 42, Second: zzz", p1.ToString());
			Assert.AreEqual("First: 0, Second: hello", p2.ToString());
			Assert.AreEqual("First: -122, Second: null", p3.ToString());
            Assert.AreEqual("First: null, Second: 11", p4.ToString());
        }

        [TestMethod]
        public void UncomparableFirst()
        {
            Pair<Unorderable, int> pair1, pair2;
            pair1 = new Pair<Unorderable, int>(new Unorderable(), 5);
            pair2 = new Pair<Unorderable, int>(new Unorderable(), 7);

            Exception ex = Assert.Throws<NotSupportedException>(() => {
                int compare = pair1.CompareTo(pair2);
            });
            Assert.AreEqual("Type \"PurplePen.Tests.PairTests+Unorderable\" does not implement IComparable<PurplePen.Tests.PairTests+Unorderable> or IComparable.", ex.Message);

        }

        [TestMethod]
        public void UncomparableSecond()
        {
            Pair<int, Unorderable> pair1, pair2;
            pair1 = new Pair<int, Unorderable>(3, new Unorderable());
            pair2 = new Pair<int, Unorderable>(3, new Unorderable());

            Exception ex = Assert.Throws<NotSupportedException>(() => {
                int compare = pair1.CompareTo(pair2);
            });
            Assert.AreEqual("Type \"PurplePen.Tests.PairTests+Unorderable\" does not implement IComparable<PurplePen.Tests.PairTests+Unorderable> or IComparable.", ex.Message);
        }

        [TestMethod]
        public void EqualUncomparable()
        {
            Pair<Unorderable, string> pair1, pair2, pair3;
            pair1 = new Pair<Unorderable, string>(new Unorderable(), "hello");
            pair2 = new Pair<Unorderable, string>(new Unorderable(), "world");
            pair3 = new Pair<Unorderable, string>(new Unorderable(), "hello");
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsTrue(pair1.Equals(pair3));
            Assert.IsFalse(pair1 == pair2);
            Assert.IsTrue(pair1 == pair3);

            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());
            Assert.IsTrue(pair1.GetHashCode() == pair3.GetHashCode());
        }

        [TestMethod]
        public void NongenericComparable()
        {
            Pair<int, OddEvenComparable> pair1, pair2;

            pair1 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(7, new OddEvenComparable(3));
            Assert.IsTrue(pair1.CompareTo(pair2) < 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(2));
            Assert.IsTrue(pair1.CompareTo(pair2) < 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            Assert.IsTrue(pair1.CompareTo(pair2) == 0);
            Assert.IsTrue(pair1.Equals(pair2));
            Assert.IsTrue(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, OddEvenComparable>(7, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(2));
            Assert.IsTrue(pair1.CompareTo(pair2) > 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, OddEvenComparable>(0, new OddEvenComparable(8));
            pair2 = new Pair<int, OddEvenComparable>(0, new OddEvenComparable(2));
            Assert.IsTrue(pair1.CompareTo(pair2) > 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            Pair<OddEvenComparable, int> pair3, pair4;

            pair3 = new Pair<OddEvenComparable, int>(new OddEvenComparable(7), 4);
            pair4 = new Pair<OddEvenComparable, int>(new OddEvenComparable(3), 7);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<OddEvenComparable, int>(new OddEvenComparable(7), 4);
            pair4 = new Pair<OddEvenComparable, int>(new OddEvenComparable(2), 4);
            Assert.IsTrue(pair3.CompareTo(pair4) < 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<OddEvenComparable, int>(new OddEvenComparable(7), 4);
            pair4 = new Pair<OddEvenComparable, int>(new OddEvenComparable(7), 4);
            Assert.IsTrue(pair3.CompareTo(pair4) == 0);
            Assert.IsTrue(pair3.Equals(pair4));
            Assert.IsTrue(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<OddEvenComparable, int>(new OddEvenComparable(2), 7);
            pair4 = new Pair<OddEvenComparable, int>(new OddEvenComparable(7), 4);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<OddEvenComparable, int>(new OddEvenComparable(8), 0);
            pair4 = new Pair<OddEvenComparable, int>(new OddEvenComparable(2), 0);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<OddEvenComparable, int>(new OddEvenComparable(2), 4);
            pair4 = new Pair<OddEvenComparable, int>(new OddEvenComparable(2), 3);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());
        }

        [TestMethod]
        public void GenericComparable()
        {
            Pair<int, GOddEvenComparable> pair1, pair2;

            pair1 = new Pair<int, GOddEvenComparable>(4, new GOddEvenComparable(7));
            pair2 = new Pair<int, GOddEvenComparable>(7, new GOddEvenComparable(3));
            Assert.IsTrue(pair1.CompareTo(pair2) < 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, GOddEvenComparable>(4, new GOddEvenComparable(7));
            pair2 = new Pair<int, GOddEvenComparable>(4, new GOddEvenComparable(2));
            Assert.IsTrue(pair1.CompareTo(pair2) < 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, GOddEvenComparable>(4, new GOddEvenComparable(7));
            pair2 = new Pair<int, GOddEvenComparable>(4, new GOddEvenComparable(7));
            Assert.IsTrue(pair1.CompareTo(pair2) == 0);
            Assert.IsTrue(pair1.Equals(pair2));
            Assert.IsTrue(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, GOddEvenComparable>(7, new GOddEvenComparable(7));
            pair2 = new Pair<int, GOddEvenComparable>(4, new GOddEvenComparable(2));
            Assert.IsTrue(pair1.CompareTo(pair2) > 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            pair1 = new Pair<int, GOddEvenComparable>(0, new GOddEvenComparable(8));
            pair2 = new Pair<int, GOddEvenComparable>(0, new GOddEvenComparable(2));
            Assert.IsTrue(pair1.CompareTo(pair2) > 0);
            Assert.IsFalse(pair1.Equals(pair2));
            Assert.IsFalse(pair1.GetHashCode() == pair2.GetHashCode());

            Pair<GOddEvenComparable, int> pair3, pair4;

            pair3 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(7), 4);
            pair4 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(3), 7);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(7), 4);
            pair4 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(2), 4);
            Assert.IsTrue(pair3.CompareTo(pair4) < 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(7), 4);
            pair4 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(7), 4);
            Assert.IsTrue(pair3.CompareTo(pair4) == 0);
            Assert.IsTrue(pair3.Equals(pair4));
            Assert.IsTrue(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(2), 7);
            pair4 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(7), 4);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(8), 0);
            pair4 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(2), 0);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());

            pair3 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(2), 4);
            pair4 = new Pair<GOddEvenComparable, int>(new GOddEvenComparable(2), 3);
            Assert.IsTrue(pair3.CompareTo(pair4) > 0);
            Assert.IsFalse(pair3.Equals(pair4));
            Assert.IsFalse(pair3.GetHashCode() == pair4.GetHashCode());
        }

        [TestMethod]
        public void DictionaryKey()
        {
            Dictionary<Pair<string, int>, string> dict1 = new Dictionary<Pair<string, int>, string>();

            dict1[new Pair<string, int>("foo", 12)] = "hello";
            dict1[new Pair<string, int>("zebra", 1)] = "long";
            dict1[new Pair<string, int>("zebra", 17)] = "strange";
            dict1[new Pair<string, int>("zzz", 14)] = "trip";
            dict1[new Pair<string, int>("foo", 16)] = "goodbye";
            dict1[new Pair<string, int>("foo", 12)] = "another";

            string[] s_array = { "another", "goodbye", "long", "strange", "trip" };

            Assert.AreEqual(5, dict1.Count);
            TestingUtils.TestUtil.TestEnumerableAnyOrder(dict1.Values, s_array);
        }

        [TestMethod]
        public void IComparable()
        {
            Pair<int, OddEvenComparable> pair1, pair2;
            IComparable comp;
            object o;

            pair1 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(7, new OddEvenComparable(3));
            comp = pair1;
            o = pair2;
            Assert.IsTrue(comp.CompareTo(o) < 0);

            pair1 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(2));
            comp = pair1;
            o = pair2;
            Assert.IsTrue(comp.CompareTo(o) < 0);

            pair1 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(7));
            comp = pair1;
            o = pair2;
            Assert.IsTrue(comp.CompareTo(o) == 0);

            pair1 = new Pair<int, OddEvenComparable>(7, new OddEvenComparable(7));
            pair2 = new Pair<int, OddEvenComparable>(4, new OddEvenComparable(2));
            comp = pair1;
            o = pair2;
            Assert.IsTrue(comp.CompareTo(o) > 0);

            try {
                int i = comp.CompareTo("foo");
                Assert.Fail("should throw");
            }
            catch (Exception e) {
                Assert.IsTrue(e is ArgumentException);
            }
        }

        [TestMethod]
        public void FromKeyValuePair()
        {
            KeyValuePair<int, string> kvp1 = new KeyValuePair<int, string>(-13, "hello");
            Pair<int, string> p1 = (Pair<int, string>)kvp1;
            Assert.AreEqual(-13, p1.First);
            Assert.AreEqual("hello", p1.Second);
            Pair<int, string> q1 = new Pair<int, string>(kvp1);
            Assert.AreEqual(-13, q1.First);
            Assert.AreEqual("hello", q1.Second);

            KeyValuePair<string, object> kvp2 = new KeyValuePair<string, object>();
            Pair<string, object> p2 = (Pair<string, object>)kvp2;
            Assert.IsNull(p2.First);
            Assert.IsNull(p2.Second);
            Pair<string, object> q2 = new Pair<string,object>(kvp2);
            Assert.IsNull(q2.First);
            Assert.IsNull(q2.Second);

            object x = new Hashtable();
            KeyValuePair<object, double> kvp3 = new KeyValuePair<object, double>(x, 6.7);
            Pair<object, double> p3 = (Pair<object, double>)kvp3;
            Assert.AreSame(x, p3.First);
            Assert.AreEqual(6.7, p3.Second);
            Pair<object, double> q3 =  new Pair<object, double>(kvp3);
            Assert.AreSame(x, q3.First);
            Assert.AreEqual(6.7, q3.Second);
        }

        [TestMethod]
        public void ToKeyValuePair()
        {
            Pair<int, string> p1 = new Pair<int, string>(-13, "hello");
            KeyValuePair<int, string> kvp1 = (KeyValuePair<int, string>)p1;
            Assert.AreEqual(-13, kvp1.Key);
            Assert.AreEqual("hello", kvp1.Value);
            KeyValuePair<int, string> kv1 = p1.ToKeyValuePair();
            Assert.AreEqual(-13, kv1.Key);
            Assert.AreEqual("hello", kv1.Value);

            Pair<string, object> p2 = new Pair<string, object>();
            KeyValuePair<string, object> kvp2 = (KeyValuePair<string, object>)p2;
            Assert.IsNull(kvp2.Key);
            Assert.IsNull(kvp2.Value);
            KeyValuePair<string, object> kv2 = p2.ToKeyValuePair();
            Assert.IsNull(kv2.Key);
            Assert.IsNull(kv2.Value);

            object x = new Hashtable();
            Pair<object, double> p3 = new Pair<object, double>(x, 6.7);
            KeyValuePair<object, double> kvp3 = (KeyValuePair<object, double>)p3;
            Assert.AreSame(x, kvp3.Key);
            Assert.AreEqual(6.7, kvp3.Value);
            KeyValuePair<object, double> kv3 = p3.ToKeyValuePair();
            Assert.AreSame(x, kv3.Key);
            Assert.AreEqual(6.7, kv3.Value);
        }
    }
}

#endif // TEST
