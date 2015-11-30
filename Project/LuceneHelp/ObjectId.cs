using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace LuceneHelp
{
    /// <summary>
    /// Represents an ObjectId (see also BsonObjectId).
    /// </summary>
    [Serializable]
    public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId>
    {
        #region private static fields
        private static ObjectId emptyInstance = default(ObjectId);
        private static int staticMachine;
        private static short staticPid;
        private static int staticIncrement; // high byte will be masked out when generating new ObjectId
        #endregion

        #region private fields
        // we're using 14 bytes instead of 12 to hold the ObjectId in memory but unlike a byte[] there is no additional object on the heap
        // the extra two bytes are not visible to anyone outside of this class and they buy us considerable simplification
        // an additional advantage of this representation is that it will serialize to JSON without any 64 bit overflow problems
        private int timestamp;
        private int machine;
        private short pid;
        private int increment;
        #endregion

        #region static constructor
        static ObjectId()
        {
            staticMachine = GetMachineHash();//获取机器名称
            staticPid = (short)Process.GetCurrentProcess().Id; // use low order two bytes only 
            //取得新的 Process 元件，並將它與目前作用中的處理序相關聯。
            staticIncrement = (new Random()).Next();//返回非负随机数
        }
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="bytes">The value.</param>
        public ObjectId(
            byte[] bytes
        )
        {
            Unpack(bytes, out timestamp, out machine, out pid, out increment);
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public ObjectId(
            int timestamp,
            int machine,
            short pid,
            int increment
        )
        {
            this.timestamp = timestamp;
            this.machine = machine;
            this.pid = pid;
            this.increment = increment;
        }

        /// <summary>
        /// Initializes a new instance of the ObjectId class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ObjectId(
            string value
        )
        {
            Unpack(Utils.ParseHexString(value), out timestamp, out machine, out pid, out increment);
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of ObjectId where the value is empty.
        /// </summary>
        public static ObjectId Empty
        {
            get { return emptyInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public int Timestamp
        {
            get { return timestamp; }
        }

        /// <summary>
        /// Gets the machine.
        /// </summary>
        public int Machine
        {
            get { return machine; }
        }

        /// <summary>
        /// Gets the PID.
        /// </summary>
        public short Pid
        {
            get { return pid; }
        }

        /// <summary>
        /// Gets the increment.
        /// </summary>
        public int Increment
        {
            get { return increment; }
        }

        /// <summary>
        /// Gets the creation time (derived from the timestamp).
        /// </summary>
        public DateTime CreationTime
        {
            get { return Constants.UnixEpoch.AddSeconds(timestamp); }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than the second ObjectId.</returns>
        public static bool operator <(
            ObjectId lhs,
            ObjectId rhs
        )
        {
            return lhs.CompareTo(rhs) < 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is less than or equal to the second ObjectId.</returns>
        public static bool operator <=(
            ObjectId lhs,
            ObjectId rhs
        )
        {
            return lhs.CompareTo(rhs) <= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public static bool operator ==(
            ObjectId lhs,
            ObjectId rhs
        )
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are not equal.</returns>
        public static bool operator !=(
            ObjectId lhs,
            ObjectId rhs
        )
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than or equal to the second ObjectId.</returns>
        public static bool operator >=(
            ObjectId lhs,
            ObjectId rhs
        )
        {
            return lhs.CompareTo(rhs) >= 0;
        }

        /// <summary>
        /// Compares two ObjectIds.
        /// </summary>
        /// <param name="lhs">The first ObjectId.</param>
        /// <param name="rhs">The other ObjectId</param>
        /// <returns>True if the first ObjectId is greather than the second ObjectId.</returns>
        public static bool operator >(
            ObjectId lhs,
            ObjectId rhs
        )
        {
            return lhs.CompareTo(rhs) > 0;
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Generates a new ObjectId with a unique value.
        /// </summary>
        /// <returns>A ObjectId.</returns>
        public static ObjectId GenerateNewId()
        {
            int timestamp = GetCurrentTimestamp();//根据时间获取
            int increment = Interlocked.Increment(ref ObjectId.staticIncrement) & 0x00ffffff; // only use low order 3 bytes
            return new ObjectId(timestamp, staticMachine, staticPid, increment);
        }

        public static string NewId()
        {
            return GenerateNewId().ToString();
        }

        /// <summary>
        /// Packs the components of an ObjectId into a byte array.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        /// <returns>A byte array.</returns>
        public static byte[] Pack(
            int timestamp,
            int machine,
            short pid,
            int increment
        )
        {
            byte[] bytes = new byte[12];
            bytes[0] = (byte)(timestamp >> 24);
            bytes[1] = (byte)(timestamp >> 16);
            bytes[2] = (byte)(timestamp >> 8);
            bytes[3] = (byte)(timestamp);
            bytes[4] = (byte)(machine >> 16);
            bytes[5] = (byte)(machine >> 8);
            bytes[6] = (byte)(machine);
            bytes[7] = (byte)(pid >> 8);
            bytes[8] = (byte)(pid);
            bytes[9] = (byte)(increment >> 16);
            bytes[10] = (byte)(increment >> 8);
            bytes[11] = (byte)(increment);
            return bytes;
        }

        /// <summary>
        /// Parses a string and creates a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <returns>A ObjectId.</returns>
        public static ObjectId Parse(
            string s
        )
        {
            ObjectId objectId;
            if (TryParse(s, out objectId))
            {
                return objectId;
            }
            else
            {
                var message = string.Format("'{0}' is not a valid 24 digit hex string.", s);
                throw new FormatException(message);
            }
        }

        /// <summary>
        /// Tries to parse a string and create a new ObjectId.
        /// </summary>
        /// <param name="s">The string value.</param>
        /// <param name="objectId">The new ObjectId.</param>
        /// <returns>True if the string was parsed successfully.</returns>
        public static bool TryParse(
            string s,
            out ObjectId objectId
        )
        {
            if (s != null && s.Length == 24)
            {
                byte[] bytes;
                if (Utils.TryParseHexString(s, out bytes))
                {
                    objectId = new ObjectId(bytes);
                    return true;
                }
            }

            objectId = default(ObjectId);
            return false;
        }

        /// <summary>
        /// Unpacks a byte array into the components of an ObjectId.
        /// </summary>
        /// <param name="bytes">A byte array.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public static void Unpack(
            byte[] bytes,
            out int timestamp,
            out int machine,
            out short pid,
            out int increment
        )
        {
            if (bytes.Length != 12)
            {
                throw new ArgumentOutOfRangeException("Byte array must be 12 bytes long.");
            }
            timestamp = (bytes[0] << 24) + (bytes[1] << 16) + (bytes[2] << 8) + bytes[3];
            machine = (bytes[4] << 16) + (bytes[5] << 8) + bytes[6];
            pid = (short)((bytes[7] << 8) + bytes[8]);
            increment = (bytes[9] << 16) + (bytes[10] << 8) + bytes[11];
        }
        #endregion

        #region private static methods
        private static int GetMachineHash()
        {
            var hostName = Environment.MachineName; // use instead of Dns.HostName so it will work offline
            var md5 = MD5.Create();//哈希算法的默认实现的实例
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(hostName));//在派生类中重写时，将指定的System.String中的所有字符编码
            return (hash[0] << 16) + (hash[1] << 8) + hash[2]; // use first 3 bytes of hash
        }

        private static int GetCurrentTimestamp()
        {
            DateTime now = DateTime.UtcNow;
            return (int)Math.Floor((now - Constants.UnixEpoch).TotalSeconds);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="other">The other ObjectId.</param>
        /// <returns>A 32-bit signed integer that indicates whether this ObjectId is less than, equal to, or greather than the other.</returns>
        public int CompareTo(
            ObjectId other
        )
        {
            int r = timestamp.CompareTo(other.timestamp);
            if (r != 0) { return r; }
            r = machine.CompareTo(other.machine);
            if (r != 0) { return r; }
            r = pid.CompareTo(other.pid);
            if (r != 0) { return r; }
            return increment.CompareTo(other.increment);
        }

        /// <summary>
        /// Compares this ObjectId to another ObjectId.
        /// </summary>
        /// <param name="rhs">The other ObjectId.</param>
        /// <returns>True if the two ObjectIds are equal.</returns>
        public bool Equals(
            ObjectId rhs
        )
        {
            return
                this.timestamp == rhs.timestamp &&
                this.machine == rhs.machine &&
                this.pid == rhs.pid &&
                this.increment == rhs.increment;
        }

        /// <summary>
        /// Compares this ObjectId to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is an ObjectId and equal to this one.</returns>
        public override bool Equals(
            object obj
        )
        {
            if (obj is ObjectId)
            {
                return Equals((ObjectId)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = 37 * hash + timestamp.GetHashCode();
            hash = 37 * hash + machine.GetHashCode();
            hash = 37 * hash + pid.GetHashCode();
            hash = 37 * hash + increment.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Converts the ObjectId to a byte array.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            return Pack(timestamp, machine, pid, increment);
        }

        /// <summary>
        /// Returns a string representation of the value.
        /// </summary>
        /// <returns>A string representation of the value.</returns>
        public override string ToString()
        {
            return Utils.ToHexString(Pack(timestamp, machine, pid, increment));
        }
        #endregion


        internal static class Utils
        {
            /// <summary>
            /// Tries to parse a hex string to a byte array.
            /// </summary>
            /// <param name="s">The hex string.</param>
            /// <param name="bytes">A byte array.</param>
            /// <returns>True if the hex string was successfully parsed.</returns>
            public static bool TryParseHexString(
                string s,
                out byte[] bytes
            )
            {
                if (s != null)
                {
                    if ((s.Length & 1) != 0) { s = "0" + s; } // make length of s even
                    bytes = new byte[s.Length / 2];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        string hex = s.Substring(2 * i, 2);
                        try
                        {
                            byte b = Convert.ToByte(hex, 16);
                            bytes[i] = b;
                        }
                        catch (FormatException)
                        {
                            bytes = null;
                            return false;
                        }
                    }
                    return true;
                }

                bytes = null;
                return false;
            }

            /// <summary>
            /// Tries to parse a hex string to a byte array.
            /// </summary>
            /// <param name="s">The hex string.</param>
            /// <param name="bytes">A byte array.</param>
            /// <returns>True if the hex string was successfully parsed.</returns>
            public static byte[] ParseHexString(string s)
            {
                byte[] bytes;
                if (!TryParseHexString(s, out bytes))
                {
                    var message = string.Format("'{0}' is not a valid hex string.", s);
                    throw new FormatException(message);
                }
                return bytes;
            }

            /// <summary>
            /// Converts a byte array to a hex string.
            /// </summary>
            /// <param name="bytes">The byte array.</param>
            /// <returns>A hex string.</returns>
            public static string ToHexString(
                byte[] bytes
            )
            {
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString();
            }
        }

        internal static class Constants
        {
            #region private static fields
            private static readonly long dateTimeMaxValueMillisecondsSinceEpoch;
            private static readonly long dateTimeMinValueMillisecondsSinceEpoch;
            private static readonly DateTime unixEpoch;
            #endregion

            #region static constructor
            static Constants()
            {
                // unixEpoch has to be initialized first
                unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                dateTimeMaxValueMillisecondsSinceEpoch = (DateTime.MaxValue - unixEpoch).Ticks / 10000;
                dateTimeMinValueMillisecondsSinceEpoch = (DateTime.MinValue - unixEpoch).Ticks / 10000;
            }
            #endregion

            #region public static properties
            /// <summary>
            /// Gets the number of milliseconds since the Unix epoch for DateTime.MaxValue.
            /// </summary>
            public static long DateTimeMaxValueMillisecondsSinceEpoch
            {
                get { return dateTimeMaxValueMillisecondsSinceEpoch; }
            }

            /// <summary>
            /// Gets the number of milliseconds since the Unix epoch for DateTime.MinValue.
            /// </summary>
            public static long DateTimeMinValueMillisecondsSinceEpoch
            {
                get { return dateTimeMinValueMillisecondsSinceEpoch; }
            }

            /// <summary>
            /// Gets the Unix Epoch for BSON DateTimes (1970-01-01).
            /// </summary>
            public static DateTime UnixEpoch { get { return unixEpoch; } }
            #endregion
        }

    }
}