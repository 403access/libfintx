﻿/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (C) 2016 - 2022 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
 *  
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 3 of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software Foundation,
 *  Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 * 	
 */

using System;
using System.Text;
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Message;
using libfintx.FinTS.Segments;
using libfintx.Logger.Log;
using libfintx.Sepa;

namespace libfintx.FinTS
{
    public static class HKCSL
    {
        /// <summary>
        /// Delete terminated transfer
        /// </summary>
        public static async Task<string> Init_HKCSL(FinTsClient client, string OrderId, string Receiver,
            string ReceiverIBAN, string ReceiverBIC, decimal Amount, string Usage, DateTime ExecutionDay)
        {
            Log.Write("Starting job HKCSL: Delete terminated transfer");

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg3);
            var connectionDetails = client.ConnectionDetails;
            SEG sEG = new SEG();
            StringBuilder sb = new StringBuilder();
            sb.Append(connectionDetails.Iban);
            sb.Append(DEG.Separator);
            sb.Append(connectionDetails.Bic);
            sb.Append("+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03+@@");
            string segments = sEG.toSEG(new SEG_DATA
            {
                Header = "HKCSL",
                Num = client.SEGNUM,
                Version = 1,
                RefNum = 0,
                RawData = sb.ToString()
            });
            //string segments = "HKCSL:" + client.SEGNUM + ":1+" + connectionDetails.Iban + ":" + connectionDetails.Bic + "+urn?:iso?:std?:iso?:20022?:tech?:xsd?:pain.001.001.03+@@";

            var sepaMessage = pain00100103.Create(connectionDetails.AccountHolder, connectionDetails.Iban,
                connectionDetails.Bic, Receiver, ReceiverIBAN, ReceiverBIC, Amount, Usage, ExecutionDay).Replace("'", "");
            segments = segments.Replace("@@", "@" + sepaMessage.Length + "@") + sepaMessage;

            segments += sEG.Delimiter + OrderId;

            segments += sEG.Terminator;

            if (client.BPD.IsTANRequired("HKCSL"))
            {
                client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg4);
                segments = HKTAN.Init_HKTAN(client, segments, "HKCSL");
            }

            string message = FinTSMessage.Create(client, client.HNHBS, client.HNHBK, segments, client.HIRMS);
            var TAN = await FinTSMessage.Send(client, message);

            Helper.Parse_Message(client, TAN);

            return TAN;
        }
    }
}
