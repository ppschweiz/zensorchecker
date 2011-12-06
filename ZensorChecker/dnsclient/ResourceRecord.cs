/*
* Bdev.Net.Dns by Rob Philpott, Big Developments Ltd. Please send all bugs/enhancements to
* rob@bigdevelopments.co.uk  This file and the code contained within is freeware and may be
* distributed and edited without restriction.
*/

using System;

namespace apophis.ZensorChecker.dnsclient
{
    /// <summary>
    /// Represents a Resource Record as detailed in RFC1035 4.1.3
    /// </summary>
    [Serializable]
    public class ResourceRecord
    {
        // private, constructor initialised fields
        private readonly string domain;
        private readonly DnsType dnsType;
        private readonly DnsClass dnsClass;
        private readonly int ttl;
        private readonly RecordBase record;

        // read only properties applicable for all records
        public string Domain
        {
            get
            {
                return domain;
            }
        }
        public DnsType Type
        {
            get
            {
                return dnsType;
            }
        }
        public DnsClass Class
        {
            get
            {
                return dnsClass;
            }
        }
        public int Ttl
        {
            get
            {
                return ttl;
            }
        }
        public RecordBase Record
        {
            get
            {
                return record;
            }
        }

        /// <summary>
        /// Construct a resource record from a pointer to a byte array
        /// </summary>
        /// <param name="pointer">the position in the byte array of the record</param>
        internal ResourceRecord(ref Pointer pointer)
        {
            // extract the domain, question type, question class and Ttl
            domain = pointer.ReadDomain();
            dnsType = (DnsType)pointer.ReadShort();
            dnsClass = (DnsClass)pointer.ReadShort();
            ttl = pointer.ReadInt();

            // the next short is the record length, we only use it for unrecognised record types
            int recordLength = pointer.ReadShort();

            // and create the appropriate RDATA record based on the dnsType
            switch (dnsType)
            {
                case DnsType.NS: record = new NSRecord(pointer); break;
                case DnsType.MX: record = new MXRecord(pointer); break;
                case DnsType.CNAME: record = new NSRecord(pointer); break;
                case DnsType.ANAME: record = new ANameRecord(pointer); break;
                case DnsType.SOA: record = new SoaRecord(pointer); break;
                default:
                    {
                        // move the pointer over this unrecognised record
                        pointer += recordLength;
                        break;
                    }
            }
        }
    }

    // Answers, Name Servers and Additional Records all share the same RR format
    [Serializable]
    public class Answer : ResourceRecord
    {
        internal Answer(ref Pointer pointer) : base(ref pointer) { }
    }

    [Serializable]
    public class NameServer : ResourceRecord
    {
        internal NameServer(ref Pointer pointer) : base(ref pointer) { }
    }

    [Serializable]
    public class AdditionalRecord : ResourceRecord
    {
        internal AdditionalRecord(ref Pointer pointer) : base(ref pointer) { }
    }
}