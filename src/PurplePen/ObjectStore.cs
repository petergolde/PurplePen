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
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.IO;

namespace PurplePen
{
    // A semi-opaque handle for ids so that we get stronger typing.
    public struct Id<T>: IEquatable<Id<T>>
    {
        public readonly int id;

        public static Id<T> None = new Id<T>(0);

        public Id(int id)
        {
            this.id = id;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj is Id<T>) {
                Id<T> other = (Id<T>) obj;
                return other.id == id;
            }
            else
                return false;
        }

        public bool  Equals(Id<T> other)
        {
            return other.id == id;
        }

        public static bool operator ==(Id<T> x, Id<T> y)
        {
            return x.id == y.id;
        }

        public static bool operator !=(Id<T> x, Id<T> y)
        {
            return x.id != y.id;
        }

        public bool IsNone 
        {
            get {return id == 0;}
        }

        public bool IsNotNone
        {
            get { return id != 0; }
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        // override ToString
        public override string ToString()
        {
            return id.ToString();
        }
}

    /// <summary>
    /// The ObjectStore class can store a particular type of object (which must
    /// derive from StorableObject). It is basically a simple hash table, indexed
    /// by Id<typeparam name="T"></typeparam>, which also does very simply XML persistence. Use one ObjectStore
    /// for each type of object.
    /// </summary>
    class ObjectStore<T>
        where T:StorableObject, new()
    {
        private Dictionary<Id<T>, T> dict = new Dictionary<Id<T>, T>();
        private UndoMgr undomgr;
        private int next = 1;
        private int changenum = 0;      // incremented on every change.

        public ObjectStore(UndoMgr undomgr)
        {
            this.undomgr = undomgr;
        }

        // Get the change number. This changes after each change.
        public int ChangeNum
        {
            get { return changenum; }
        }

        /// <summary>
        /// Add an object into the store, the id of the object is returned.
        /// It is cloned before being stored.
        /// </summary>
        public Id<T> Add(T obj)
        {
            Id<T> id = new Id<T>(next);
            T newobj = (T)obj.Clone();
            dict.Add(id, newobj);
            next = next + 1;
            ++changenum;

            undomgr.RecordAction(new ObjectStoreChange(this, id, null, newobj));

            return id;
        }

        /// <summary>
        /// Remove the object with the given id from the store. 
        /// </summary>
        public void Remove(Id<T> id)
        {
            Debug.Assert(dict.ContainsKey(id));

            T oldobj = dict[id];
            dict.Remove(id);
            ++changenum;
            undomgr.RecordAction(new ObjectStoreChange(this, id, oldobj, null));
        }

        /// <summary>
        /// Replace the object with given id with a new object. It is cloned
        /// before being stored.
        /// </summary>
        public void Replace(Id<T> id, T obj)
        {
            Debug.Assert(dict.ContainsKey(id));

            T oldobj = dict[id];
            T newobj = (T)obj.Clone();
            dict[id] = newobj;
            ++changenum;
            undomgr.RecordAction(new ObjectStoreChange(this, id, oldobj, newobj));
        }

        /// <summary>
        /// Get the object with the given id. It is NOT cloned before being returned,
        /// so care must be made to not change it.
        /// </summary>
        public T this[Id<T> id] {
            get {
                return dict[id];
            }
        }

        /// <summary>
        /// Check if the given id is present.
        /// </summary>
        public bool IsPresent(Id<T> id)
        {
            return dict.ContainsKey(id);
        }

        /// <summary>
        /// Throw exception if the give id is not present.
        /// </summary>
        public void CheckPresent(Id<T> id)
        {
            if (!IsPresent(id))
                throw new ApplicationException(string.Format("Object of type {0} with id {1} is not present", typeof(T).Name, id));
        }

        /// <summary>
        /// Return a collection of all the object.
        /// </summary>
        public ICollection<T> All
        {
            get
            {
                return dict.Values;
            }
        }

        // Enumerate all the ids
        public ICollection<Id<T>> AllIds
        {
            get
            {
                return dict.Keys;
            }
        }

        /// <summary>
        /// Enumerable all the objects with their keys.
        /// </summary>
        public IEnumerable<KeyValuePair<Id<T>, T>> AllPairs
        {
            get
            {
                return dict;
            }
        }

        /// <summary>
        /// Load all of the objects from an XmlInput. It reads
        /// as many of the correct elements it can find in a row, and
        /// stops at the first element with the wrong element name.
        /// </summary>
        public void Load(XmlInput xmlinput)
        {
            T temp = new T();
            string element = temp.ElementName;

            xmlinput.MoveToContent();

            while (xmlinput.Name == element) {
                Id<T> id = new Id<T>(xmlinput.GetAttributeInt("id"));
                T obj = new T();
                obj.ReadAttributesAndContent(xmlinput);
                if (IsPresent(id)) 
                    xmlinput.BadXml("Duplicate id '{0}'", id);

                dict[id] = obj;
                if (id.id + 1 > next)
                    next = id.id + 1;

                xmlinput.MoveToContent();
            }
            ++changenum;
        }

        /// <summary>
        /// Save all the object to a XmlTestWriter. It writes
        /// one element for each object.
        /// </summary>
        public void Save(XmlTextWriter xmloutput)
        {
            foreach (KeyValuePair<Id<T>, T> pair in dict) {
                xmloutput.WriteStartElement(pair.Value.ElementName);
                xmloutput.WriteAttributeString("id", XmlConvert.ToString(pair.Key.id));
                pair.Value.WriteAttributesAndContent(xmloutput);
                xmloutput.WriteEndElement();
            }
        }


        /// <summary>
        /// This is the action entered into the UndoMgr for
        /// any change made to the object store. The before and
        /// after can both be null for an insert/remove respectively.
        /// </summary>
        private class ObjectStoreChange : UndoableAction
        {
            private ObjectStore<T> objectstore;
            private Id<T> id;
            private T before, after;

            public ObjectStoreChange(ObjectStore<T> objectstore, Id<T> id, T before, T after)
            {
                this.objectstore = objectstore;
                this.id = id;
                this.before = before;
                this.after = after;
            }

            public override void Undo()
            {
                Modify(id, after, before);
            }

            public override void Redo()
            {
                Modify(id, before, after);
            }

            private void Modify(Id<T> id, T from, T to)
            {
                if (to == null)
                    objectstore.dict.Remove(id);
                else
                    objectstore.dict[id] = to;

                ++objectstore.changenum;
            }
        }
    }

    /// <summary>
    /// The abstract class that all objects in an ObjectStore must conform to.
    /// </summary>
    public abstract class StorableObject
    {
        public virtual StorableObject Clone()
        {
            return (StorableObject) this.MemberwiseClone();
        }

        /// <summary>
        /// The input is position on an element with name ElementName.
        /// Read the attributes, content, and leave the input positioned on the next element.
        /// </summary>
        public abstract void ReadAttributesAndContent(XmlInput xmlinput);

        /// <summary>
        /// A start element has been written to the output. Write any attributes and content.
        /// Don't write the end element.
        /// </summary>
        public abstract void WriteAttributesAndContent(XmlTextWriter xmloutput);

        /// <summary>
        /// Get the element name for load and save. Must be constant for all objects of this type.
        /// </summary>
        public abstract string ElementName { get;} 
    }

}
