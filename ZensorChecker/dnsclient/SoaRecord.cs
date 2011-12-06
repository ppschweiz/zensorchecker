/*
* Bdev.Net.Dns by Rob Philpott, Big Developments Ltd. Please send all bugs/enhancements to
* rob@bigdevelopments.co.uk  This file and the code contained within is freeware and may be
* distributed and edited without restriction.
*/

namespace apophis.ZensorChecker.dnsclient
{
    /// <summary>
    /// An SOA Resource Record (RR) (RFC1035 3.3.13)
    /// </summary>
    public class SoaRecord : RecordBase
    {
        // these fields constitute an SOA RR
        private readonly string primaryNameServer;
        private readonly string responsibleMailAddress;
        private readonly int serial;
        private readonly int refresh;
        private readonly int retry;
        private readonly int expire;
        private readonly int defaultTtl;

        // expose these fields public read/only
        public string PrimaryNameServer
        {
            get
            {
                return primaryNameServer;
            }
        }
        public string ResponsibleMailAddress
        {
            get
            {
                return responsibleMailAddress;
            }
        }
        public int Serial
        {
            get
            {
                return serial;
            }
        }
        public int Refresh
        {
            get
            {
                return refresh;
            }
        }
        public int Retry
        {
            get
            {
                return retry;
            }
        }
        public int Expire
        {
            get
            {
                return expire;
            }
        }
        public int DefaultTtl
        {
            get
            {
                return defaultTtl;
            }
        }

        /// <summary>
        /// Constructs an SOA record by reading bytes from a return message
        /// </summary>
        /// <param name="pointer">A logical pointer to the bytes holding the record</param>
        internal SoaRecord(Pointer pointer)
        {
            // read all fields RFC1035 3.3.13
            primaryNameServer = pointer.ReadDomain();
            responsibleMailAddress = pointer.ReadDomain();
            serial = pointer.ReadInt();
            refresh = pointer.ReadInt();
            retry = pointer.ReadInt();
            expire = pointer.ReadInt();
            defaultTtl = pointer.ReadInt();
        }

        public override string ToString()
        {
            return string.Format("primary name server = {0}\nresponsible mail addr = {1}\nserial  = {2}\nrefresh = {3}\nretry   = {4}\nexpire  = {5}\ndefault TTL = {6}",
                primaryNameServer,
                responsibleMailAddress,
                serial,
                refresh,
                retry,
                expire,
                defaultTtl);
        }
    }
}
