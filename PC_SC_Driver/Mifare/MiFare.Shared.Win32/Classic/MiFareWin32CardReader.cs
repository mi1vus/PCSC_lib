using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiFare.PcSc;
using System.Threading.Tasks;
using MiFare.Devices;
using MiFare.PcSc.Iso7816;
using ApduResponse = MiFare.PcSc.Iso7816.ApduResponse;

namespace MiFare.Classic
{
    internal class MiFareWin32CardReader :  MiFareStandardCardReaderBase
    {
        public SmartCard SmartCard
        {
            get
            {
                return smartCard;
            }
        }
        private readonly SmartCard smartCard;
        private SmartCardConnection connection;
        private readonly Task initialization;
        private static readonly Task<ApduResponse> completed = TaskEx.FromResult<ApduResponse>(new NoResponse());

        public MiFareWin32CardReader(SmartCard smartCard, ICollection<SectorKeySet> keys) : base(keys)
        {
            this.smartCard = smartCard;

            initialization = Initialize();
        }

        private async Task Initialize()
        {
            connection = await smartCard.ConnectAsync();
        }

        protected override Task<byte[]> GetAnswerToResetAsync()
        {
            var atr = smartCard.AtrBytes;

            return TaskEx.FromResult(atr);
        }
        
        protected override async Task<ApduResponse> TransceiveAsync(ApduCommand apduCommand)
        {
            await initialization;

            return await (connection?.TransceiveAsync(apduCommand) ?? completed);
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine("Dispose: " + nameof(MiFareWin32CardReader));
            if (disposing)
            {
                connection?.Dispose();
                connection = null;
            }

            base.Dispose(disposing);
        }
    }
}