/*
* Bdev.Net.Dns by Rob Philpott, Big Developments Ltd. Please send all bugs/enhancements to
* rob@bigdevelopments.co.uk  This file and the code contained within is freeware and may be
* distributed and edited without restriction.
*/


namespace apophis.ZensorChecker.dnsclient
{
    /// <summary>
    /// A Name Server Resource Record (RR) (RFC1035 3.3.11)
    /// </summary>
    public class NSRecord : RecordBase
    {
        // the fields exposed outside the assembly
        private readonly string domainName;

        // expose this domain name address r/o to the world
        public string DomainName
        {
            get
            {
                return domainName;
            }
        }

        /// <summary>
        /// Constructs a NS record by reading bytes from a return message
        /// </summary>
        /// <param name="pointer">A logical pointer to the bytes holding the record</param>
        internal NSRecord(Pointer pointer)
        {
            domainName = pointer.ReadDomain();
        }

        public override string ToString()
        {
            return domainName;
        }
    }
}