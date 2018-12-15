/*
    Copyright(c) 2014-2018 FOG Project

    The MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Runtime.Serialization;

namespace Zazzles.Core.PubSub.IPC
{
    // Source: https://stackoverflow.com/a/9809872
    [DataContract]
    public class SerializableType
    {
        public Type type;

        // when serializing, store as a string
        [DataMember]
        string TypeString
        {
            get
            {
                if (type == null)
                    return null;
                return type.AssemblyQualifiedName;
            }
            set
            {
                if (value == null)
                    type = null;
                else
                {
                    type = Type.GetType(value);
                }
            }
        }

        // constructors
        public SerializableType()
        {
            type = null;
        }
        public SerializableType(Type t)
        {
            type = t;
        }

        // allow SerializableType to implicitly be converted to and from System.Type
        static public implicit operator Type(SerializableType stype)
        {
            return stype.type;
        }
        static public implicit operator SerializableType(Type t)
        {
            return new SerializableType(t);
        }

        // overload the == and != operators
        public static bool operator ==(SerializableType a, SerializableType b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.type == b.type;
        }
        public static bool operator !=(SerializableType a, SerializableType b)
        {
            return !(a == b);
        }
        // we don't need to overload operators between SerializableType and System.Type because we already enabled them to implicitly convert

        public override int GetHashCode()
        {
            return type.GetHashCode();
        }

        // overload the .Equals method
        public override bool Equals(Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to SerializableType return false.
            SerializableType p = obj as SerializableType;
            if ((Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (type == p.type);
        }
        public bool Equals(SerializableType p)
        {
            // If parameter is null return false:
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (type == p.type);
        }
    }
}
