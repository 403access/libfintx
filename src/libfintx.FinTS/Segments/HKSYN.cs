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
using libfintx.Globals;
using libfintx.Logger.Log;

namespace libfintx.FinTS
{
    public static class HKSYN
    {
        public static async Task<String> Init_HKSYN(FinTsClient client, int? bpdVersion = null)
        {
            Log.Write("Starting Synchronisation");

            string segments;
            var connectionDetails = client.ConnectionDetails;

            if (connectionDetails.FinTSVersion == FinTsVersion.v220)
            {
                SEG sEG = new SEG();
                var sb = new StringBuilder();
                sb.Append("HKIDN");
                sb.Append(DEG.Separator);
                sb.Append(SEG_NUM.Seg3);
                sb.Append(DEG.Separator);
                sb.Append("2");
                sb.Append(sEG.Delimiter);
                sb.Append(SEG_COUNTRY.Germany);
                sb.Append(DEG.Separator);
                sb.Append(connectionDetails.BlzPrimary);
                sb.Append(sEG.Delimiter);
                sb.Append(connectionDetails.UserIdEscaped);
                sb.Append(sEG.Delimiter);
                sb.Append("0");
                sb.Append(sEG.Delimiter);
                sb.Append("1");
                sb.Append(sEG.Terminator);

                sb.Append("HKVVB");
                sb.Append(DEG.Separator);
                sb.Append(SEG_NUM.Seg4);
                sb.Append(DEG.Separator);
                sb.Append("2");
                sb.Append(sEG.Delimiter);
                sb.Append(bpdVersion ?? 0);  // BDP-Version
                sb.Append(sEG.Delimiter);
                sb.Append("0");  // UPD-Version
                sb.Append(sEG.Delimiter);
                sb.Append("0");  // Dialogsprache
                sb.Append(sEG.Delimiter);
                sb.Append(FinTsGlobals.ProductId);  // Produktbezeichnung
                sb.Append(sEG.Delimiter);
                sb.Append(FinTsGlobals.Version);    // Produktversion
                sb.Append(sEG.Terminator);

                sb.Append("HKSYN");
                sb.Append(DEG.Separator);
                sb.Append(SEG_NUM.Seg5);
                sb.Append(DEG.Separator);
                sb.Append("2");
                sb.Append(sEG.Delimiter);
                sb.Append("0");
                sb.Append(sEG.Terminator);

                segments = sb.ToString();

                /*string segments_ =
                    "HKIDN:" + SEG_NUM.Seg3 + ":2+" + SEG_COUNTRY.Germany + ":" + connectionDetails.BlzPrimary + "+" + connectionDetails.UserId + "+0+1'" +
                    "HKVVB:" + SEG_NUM.Seg4 + ":2+0+0+0+" + FinTsGlobals.ProductId + "+" + FinTsGlobals.Version + "'" +
                    "HKSYN:" + SEG_NUM.Seg5 + ":2+0'";

                segments = segments_;*/
            }
            else if (connectionDetails.FinTSVersion == FinTsVersion.v300)
            {
                SEG sEG = new SEG();
                var sb = new StringBuilder();
                sb.Append("HKIDN");
                sb.Append(DEG.Separator);
                sb.Append(SEG_NUM.Seg3);
                sb.Append(DEG.Separator);
                sb.Append("2");
                sb.Append(sEG.Delimiter);
                sb.Append(SEG_COUNTRY.Germany);
                sb.Append(DEG.Separator);
                sb.Append(connectionDetails.BlzPrimary);
                sb.Append(sEG.Delimiter);
                sb.Append(connectionDetails.UserIdEscaped);
                sb.Append(sEG.Delimiter);
                sb.Append("0");
                sb.Append(sEG.Delimiter);
                sb.Append("1");
                sb.Append(sEG.Terminator);

                sb.Append("HKVVB");
                sb.Append(DEG.Separator);
                sb.Append(SEG_NUM.Seg4);
                sb.Append(DEG.Separator);
                sb.Append("3");
                sb.Append(sEG.Delimiter);
                sb.Append(bpdVersion ?? 0);  // BDP-Version
                sb.Append(sEG.Delimiter);
                sb.Append("0");  // UPD-Version
                sb.Append(sEG.Delimiter);
                sb.Append("0");  // Dialogsprache
                sb.Append(sEG.Delimiter);
                sb.Append(FinTsGlobals.ProductId);  // Produktbezeichnung
                sb.Append(sEG.Delimiter);
                sb.Append(FinTsGlobals.Version);    // Produktversion
                sb.Append(sEG.Terminator);

                sb.Append("HKSYN");
                sb.Append(DEG.Separator);
                sb.Append(SEG_NUM.Seg5);
                sb.Append(DEG.Separator);
                sb.Append("3");
                sb.Append(sEG.Delimiter);
                sb.Append("0");
                sb.Append(sEG.Terminator);

                segments = sb.ToString();

                /*string segments_ =
                    "HKIDN:" + SEG_NUM.Seg3 + ":2+" + SEG_COUNTRY.Germany + ":" + connectionDetails.BlzPrimary + "+" + connectionDetails.UserId + "+0+1'" +
                    "HKVVB:" + SEG_NUM.Seg4 + ":3+0+0+0+" + FinTsGlobals.ProductId + "+" + FinTsGlobals.Version + "'" +
                    "HKSYN:" + SEG_NUM.Seg5 + ":3+0'";

                segments = segments_;*/
            }
            else
            {
                //Since connectionDetails is a re-usable object, this shouldn't be cleared.
                //connectionDetails.UserId = string.Empty;
                //connectionDetails.Pin = null;

                Log.Write("HBCI version not supported");

                throw new Exception("HBCI version not supported");
            }

            client.SEGNUM = Convert.ToInt16(SEG_NUM.Seg5);

            string message = FinTSMessage.CreateSync(client, segments);
            string response = await FinTSMessage.Send(client, message);

            client.Parse_Segments(response);

            return response;
        }
    }
}
