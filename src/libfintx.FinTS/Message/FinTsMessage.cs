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
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using libfintx.FinTS.Data;
using libfintx.FinTS.Exceptions;
using libfintx.FinTS.Security;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS.Message
{
    public abstract class FinTSMessage
    {

        /// <summary>
        /// Create FinTS message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="Segments"></param>
        /// <param name="SegmentNum"></param>
        /// <returns></returns>

        public static string CreateSync(FinTsClient client, string Segments)
        {
            return Create(client, 1, "0", Segments, null, "0");
        }
        /// <summary>
        /// Create FinTS message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="MsgNum"></param>
        /// <param name="DialogID"></param>
        /// <param name="Segments"></param>
        /// <param name="HIRMS_TAN"></param>
        /// <param name="SystemID"></param>
        /// <returns></returns>
        ///
        /// (iwen65) First redesign to make things easier and more readable. All important params were values that had been stored as properties of the FinTsClient
        /// If this is connected as close as this it is better to pass the client as ref parameter, that makes the hole method much more flexible and extensible
        /// without breaking changes.
        /// ConnectionDetails are a part of the client too. We can handle the new added ServiceProtocolType inside the common connectioninfo without breaking changes too.
        /// Mostly it will be TLS12 but who knows.
        /// I'm pretty sure the method can be simplified even more. 
        /// 
        public static string Create(FinTsClient client, int MsgNum, string DialogID, string Segments, string HIRMS_TAN, string SystemID = null)
        {

            FinTsVersion version = client.ConnectionDetails.FinTSVersion;
            int BLZ = client.ConnectionDetails.BlzPrimary;
            //string UserID = client.ConnectionDetails.UserId;
            // Änderung aufgrund https://github.com/abid76/libfintx/commit/95e8e4768d94a42f91a7aa4c905b88bae0439827
            string UserID = client.ConnectionDetails.UserIdEscaped;
            string PIN = client.ConnectionDetails.Pin;
            int SegmentNum = client.SEGNUM;

            if (SystemID == null)
                SystemID = client.SystemId; 

            if (MsgNum == 0)
                MsgNum = 1;

            DialogID += "";

            var HEAD_LEN = 29;
            var TRAIL_LEN = 11;

            Random Rnd = new Random();
            int RndNr = Rnd.Next();

            var encHead = string.Empty;
            var sigHead = string.Empty;
            var sigTrail = string.Empty;

            var secRef = Math.Round(Convert.ToDecimal(RndNr.ToString().Replace("-", "")) * 999999 + 1000000);

            string date = Convert.ToString(DateTime.Now.Year) + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd");
            string time = Convert.ToString(DateTime.Now.TimeOfDay).Substring(0, 8).Replace(":", "");

            string TAN_ = string.Empty;

            StringBuilder sb;

            SEG sEG = new SEG();
            
            if (HIRMS_TAN != null)
            {
                if (HIRMS_TAN.Length >= 10)
                {
                    var split = HIRMS_TAN.Split(':');
                    if (split.Length == 2)
                    {
                        HIRMS_TAN = split[0];
                        TAN_ = ":" + split[1];
                    }
                }
            }

            if (version == FinTsVersion.v220)
            {
                sb = new StringBuilder();
                sb.Append("HNVSK");
                sb.Append(DEG.Separator);
                sb.Append(Enc.SECFUNC_ENC_PLAIN);
                sb.Append(DEG.Separator);
                sb.Append("2");
                sb.Append(sEG.Delimiter);
                sb.Append(Enc.SECFUNC_ENC_PLAIN);
                sb.Append(sEG.Delimiter);
                sb.Append("1");
                sb.Append(sEG.Delimiter);
                sb.Append("1");
                sb.Append(DEG.Separator);
                sb.Append(DEG.Separator);
                sb.Append(SystemID);
                sb.Append(sEG.Delimiter);
                sb.Append("1");
                sb.Append(DEG.Separator);
                sb.Append(date);
                sb.Append(DEG.Separator);
                sb.Append(time);
                sb.Append(sEG.Delimiter);
                sb.Append("2");
                sb.Append(DEG.Separator);
                sb.Append("2");
                sb.Append(DEG.Separator);
                sb.Append(Enc.ENCALG_2K3DES);
                sb.Append(DEG.Separator);
                sb.Append("@8@00000000");
                sb.Append(DEG.Separator);
                sb.Append(Sig.HASHALG_SHA512);
                sb.Append(DEG.Separator);
                sb.Append("1");
                sb.Append(sEG.Delimiter);
                sb.Append(SEG_COUNTRY.Germany);
                sb.Append(DEG.Separator);
                sb.Append(BLZ);
                sb.Append(DEG.Separator);
                sb.Append(UserID);
                sb.Append(DEG.Separator);
                sb.Append("V");
                sb.Append(DEG.Separator);
                sb.Append("0");
                sb.Append(DEG.Separator);
                sb.Append("0");
                sb.Append(sEG.Delimiter);
                sb.Append("0");
                sb.Append(sEG.Terminator);
                encHead = sb.ToString();
                //encHead = "HNVSK:" + Enc.SECFUNC_ENC_PLAIN + ":2+" + Enc.SECFUNC_ENC_PLAIN + "+1+1::" + SystemID + "+1:" + date + ":" + time + "+2:2:13:@8@00000000:5:1+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":V:0:0+0'";

                client.Logger.LogInformation(encHead.Replace(UserID, "XXXXXX"));

                sigHead = string.Empty;

                if (HIRMS_TAN == null)
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHK");
                    sb.Append(DEG.Separator);
                    sb.Append("2");
                    sb.Append(DEG.Separator);
                    sb.Append("3");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Sig.SECFUNC_SIG_PT_2STEP_MIN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(DEG.Separator);
                    sb.Append(SystemID);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(date);
                    sb.Append(DEG.Separator);
                    sb.Append(time);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_RETAIL_MAC);
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Sig.HASHALG_SHA256_SHA256);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGALG_RSA);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_ISO9796_1);
                    sb.Append(sEG.Delimiter);
                    sb.Append(SEG_COUNTRY.Germany);
                    sb.Append(DEG.Separator);
                    sb.Append(BLZ);
                    sb.Append(DEG.Separator);
                    sb.Append(UserID);
                    sb.Append(DEG.Separator);
                    sb.Append("S");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(sEG.Terminator);
                    sigHead = sb.ToString();
                    // sigHead = "HNSHK:2:3+" + Sig.SECFUNC_SIG_PT_2STEP_MIN + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:" + Sig.SIGMODE_RETAIL_MAC + ":1 +6:10:16+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    client.Logger.LogInformation(sigHead.Replace(UserID, "XXXXXX"));
                }

                else
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHK");
                    sb.Append(DEG.Separator);
                    sb.Append("2");
                    sb.Append(DEG.Separator);
                    sb.Append("3");
                    sb.Append(sEG.Delimiter);
                    sb.Append(HIRMS_TAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(DEG.Separator);
                    sb.Append(SystemID);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(date);
                    sb.Append(DEG.Separator);
                    sb.Append(time);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_RETAIL_MAC);
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Sig.HASHALG_SHA256_SHA256);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGALG_RSA);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_ISO9796_1);
                    sb.Append(sEG.Delimiter);
                    sb.Append(SEG_COUNTRY.Germany);
                    sb.Append(DEG.Separator);
                    sb.Append(BLZ);
                    sb.Append(DEG.Separator);
                    sb.Append(UserID);
                    sb.Append(DEG.Separator);
                    sb.Append("S");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(sEG.Terminator);
                    sigHead = sb.ToString();
                    // sigHead = "HNSHK:2:3+" + HIRMS_TAN + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:" + Sig.SIGMODE_RETAIL_MAC + ":1+6:10:16+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    client.Logger.LogInformation(sigHead.Replace(UserID, "XXXXXX"));
                }

                if (String.IsNullOrEmpty(TAN_))
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(PIN);
                    sb.Append(sEG.Terminator);
                    sigTrail = sb.ToString();
                    //sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":1+" + secRef + "++" + PIN + "'";

                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append("XXXXXX");
                    sb.Append(sEG.Terminator);

                    client.Logger.LogInformation(sb.ToString());
                }

                else
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(PIN);
                    sb.Append(TAN_);
                    sb.Append(sEG.Terminator);
                    sigTrail = sb.ToString();
                    //sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":1+" + secRef + "++" + PIN + TAN_ + "'";

                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append("XXXXXX");
                    sb.Append("XXXXXX");
                    sb.Append(sEG.Terminator);

                    client.Logger.LogInformation(sb.ToString());
                }
            }
            else if (version == FinTsVersion.v300)
            {
                if (HIRMS_TAN == null)
                {
                    sb = new StringBuilder();
                    sb.Append("HNVSK");
                    sb.Append(DEG.Separator);
                    sb.Append(Enc.SECFUNC_ENC_PLAIN);
                    sb.Append(DEG.Separator);
                    sb.Append("3");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Step.SEC);
                    sb.Append(DEG.Separator);
                    sb.Append(Step.PIN_STEP_ONE);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Enc.SECFUNC_ENC_PLAIN);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(DEG.Separator);
                    sb.Append(SystemID);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(date);
                    sb.Append(DEG.Separator);
                    sb.Append(time);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SECFUNC_FINTS_SIG_SIG);
                    sb.Append(DEG.Separator);
                    sb.Append(Enc.ENCALG_2K3DES);
                    sb.Append(DEG.Separator);
                    sb.Append("@8@00000000");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.HASHALG_SHA512);
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(SEG_COUNTRY.Germany);
                    sb.Append(DEG.Separator);
                    sb.Append(BLZ);
                    sb.Append(DEG.Separator);
                    sb.Append(UserID);
                    sb.Append(DEG.Separator);
                    sb.Append("V");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(sEG.Delimiter);
                    sb.Append("0");
                    sb.Append(sEG.Terminator);
                    encHead = sb.ToString();
                    // encHead = "HNVSK:" + Enc.SECFUNC_ENC_PLAIN + ":3+PIN:1+" + Enc.SECFUNC_ENC_PLAIN + "+1+1::" + SystemID + "+1:" + date + ":" + time + "+2:2:13:@8@00000000:5:1+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":V:0:0+0'";
                }
                    
                else
                {
                    sb = new StringBuilder();
                    sb.Append("HNVSK");
                    sb.Append(DEG.Separator);
                    sb.Append(Enc.SECFUNC_ENC_PLAIN);
                    sb.Append(DEG.Separator);
                    sb.Append("3");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Step.SEC);
                    sb.Append(DEG.Separator);
                    sb.Append(Step.PIN_STEP_TWO);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Enc.SECFUNC_ENC_PLAIN);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(DEG.Separator);
                    sb.Append(SystemID);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(date);
                    sb.Append(DEG.Separator);
                    sb.Append(time);
                    sb.Append(sEG.Delimiter);
                    sb.Append("2");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SECFUNC_FINTS_SIG_SIG);
                    sb.Append(DEG.Separator);
                    sb.Append(Enc.ENCALG_2K3DES);
                    sb.Append(DEG.Separator);
                    sb.Append("@8@00000000");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.HASHALG_SHA512);
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(SEG_COUNTRY.Germany);
                    sb.Append(DEG.Separator);
                    sb.Append(BLZ);
                    sb.Append(DEG.Separator);
                    sb.Append(UserID);
                    sb.Append(DEG.Separator);
                    sb.Append("V");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(sEG.Delimiter);
                    sb.Append("0");
                    sb.Append(sEG.Terminator);
                    encHead = sb.ToString();
                    // encHead = "HNVSK:" + Enc.SECFUNC_ENC_PLAIN + ":3+PIN:2+" + Enc.SECFUNC_ENC_PLAIN + "+1+1::" + SystemID + "+1:" + date + ":" + time + "+2:2:13:@8@00000000:5:1+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":V:0:0+0'";
                }
                    
                client.Logger.LogInformation(encHead.Replace(UserID, "XXXXXX"));

                if (HIRMS_TAN == null)
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHK");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SECFUNC_FINTS_SIG_SIG);
                    sb.Append(DEG.Separator);
                    sb.Append(Enc.SECFUNC_ENC_3DES);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Step.SEC);
                    sb.Append(DEG.Separator);
                    sb.Append(Step.PIN_STEP_ONE);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Sig.SECFUNC_SIG_PT_1STEP);
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(DEG.Separator);
                    sb.Append(SystemID);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(date);
                    sb.Append(DEG.Separator);
                    sb.Append(time);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_RETAIL_MAC);
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Sig.HASHALG_SHA256_SHA256);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGALG_RSA);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_ISO9796_1);
                    sb.Append(sEG.Delimiter);
                    sb.Append(SEG_COUNTRY.Germany);
                    sb.Append(DEG.Separator);
                    sb.Append(BLZ);
                    sb.Append(DEG.Separator);
                    sb.Append(UserID);
                    sb.Append(DEG.Separator);
                    sb.Append("S");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(sEG.Terminator);
                    sigHead = sb.ToString();
                    //sigHead = "HNSHK:2:4+PIN:1+" + Sig.SECFUNC_SIG_PT_1STEP + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:" + Sig.SIGMODE_RETAIL_MAC + ":1+6:10:16+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    client.Logger.LogInformation(sigHead.Replace(UserID, "XXXXXX"));
                }
                else
                {
                    var SECFUNC = HIRMS_TAN.Equals("999") ? "1" : "2";

                    sb = new StringBuilder();
                    sb.Append("HNSHK");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SECFUNC_FINTS_SIG_SIG);
                    sb.Append(DEG.Separator);
                    sb.Append(Enc.SECFUNC_ENC_3DES);
                    sb.Append(sEG.Delimiter);
                    sb.Append(Step.SEC);
                    sb.Append(DEG.Separator);
                    sb.Append(SECFUNC);
                    sb.Append(sEG.Delimiter);
                    sb.Append(HIRMS_TAN);
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(DEG.Separator);
                    sb.Append(SystemID);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(date);
                    sb.Append(DEG.Separator);
                    sb.Append(time);
                    sb.Append(sEG.Delimiter);
                    sb.Append("1");
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_RETAIL_MAC);
                    sb.Append(DEG.Separator);
                    sb.Append("1");
                    sb.Append(sEG.Delimiter);
                    sb.Append(Sig.HASHALG_SHA256_SHA256);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGALG_RSA);
                    sb.Append(DEG.Separator);
                    sb.Append(Sig.SIGMODE_ISO9796_1);
                    sb.Append(sEG.Delimiter);
                    sb.Append(SEG_COUNTRY.Germany);
                    sb.Append(DEG.Separator);
                    sb.Append(BLZ);
                    sb.Append(DEG.Separator);
                    sb.Append(UserID);
                    sb.Append(DEG.Separator);
                    sb.Append("S");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(DEG.Separator);
                    sb.Append("0");
                    sb.Append(sEG.Terminator);
                    sigHead = sb.ToString();
                    // sigHead = "HNSHK:2:4+PIN:" + SECFUNC + "+" + HIRMS_TAN + "+" + secRef + "+1+1+1::" + SystemID + "+1+1:" + date + ":" + time + "+1:" + Sig.SIGMODE_RETAIL_MAC + ":1+6:10:16+" + SEG_COUNTRY.Germany + ":" + BLZ + ":" + UserID + ":S:0:0'";

                    client.Logger.LogInformation(sigHead.Replace(UserID, "XXXXXX"));
                }

                if (String.IsNullOrEmpty(TAN_))
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(PIN);
                    sb.Append(sEG.Terminator);
                    sigTrail = sb.ToString();
                    //sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":2+" + secRef + "++" + PIN + "'";

                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append("XXXXXX");
                    sb.Append(sEG.Terminator);

                    client.Logger.LogInformation(sb.ToString());
                }

                else
                {
                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append(PIN);
                    sb.Append(TAN_);
                    sb.Append(sEG.Terminator);
                    sigTrail = sb.ToString();
                    //sigTrail = "HNSHA:" + Convert.ToString(SegmentNum + 1) + ":2+" + secRef + "++" + PIN + TAN_ + "'";

                    sb = new StringBuilder();
                    sb.Append("HNSHA");
                    sb.Append(DEG.Separator);
                    sb.Append(Convert.ToString(SegmentNum + 1));
                    sb.Append(DEG.Separator);
                    sb.Append("2");
                    sb.Append(sEG.Delimiter);
                    sb.Append(secRef);
                    sb.Append(sEG.Delimiter);
                    sb.Append(sEG.Delimiter);
                    sb.Append("XXXXXX");
                    sb.Append("XXXXXX");
                    sb.Append(sEG.Terminator);

                    client.Logger.LogInformation(sb.ToString());
                }
            }
            else
            {
                throw new FinTsVersionNotSupportedException(version,
                    new[] { FinTsVersion.v220, FinTsVersion.v300 });
            }

            Segments = sigHead + Segments + sigTrail;

            var payload = Helper.Encrypt(Segments);

            if (!string.IsNullOrEmpty(payload))
            {
                if (HIRMS_TAN == null)
                {
                    client.Logger.LogInformation(payload.Replace(UserID, "XXXXXX").Replace(PIN, "XXXXXX"));
                }
                else if (!string.IsNullOrEmpty(TAN_))
                {
                    client.Logger.LogInformation(payload.Replace(UserID, "XXXXXX").Replace(PIN, "XXXXXX").Replace(TAN_, "XXXXXX"));
                }
            }

            var msgLen = HEAD_LEN + TRAIL_LEN + ($"{MsgNum}".Length * 2) + DialogID.Length + payload.Length + encHead.Length;

            var paddedLen = ("000000000000").Substring(0, 12 - Convert.ToString(msgLen).Length) + Convert.ToString(msgLen);

            var msgHead = string.Empty;

            if (version == FinTsVersion.v220)
            {
                sb = new StringBuilder();
                sb.Append("HNHBK");
                sb.Append(DEG.Separator);
                sb.Append("1");
                sb.Append(DEG.Separator);
                sb.Append("3");
                sb.Append(sEG.Delimiter);
                sb.Append(paddedLen);
                sb.Append(sEG.Delimiter);
                sb.Append((int) FinTsVersion.v220);
                sb.Append(sEG.Delimiter);
                sb.Append(DialogID);
                sb.Append(sEG.Delimiter);
                sb.Append(MsgNum);
                sb.Append(sEG.Terminator);
                msgHead = sb.ToString();
                //msgHead = "HNHBK:1:3+" + paddedLen + "+" + (HBCI.v220) + "+" + DialogID + "+" + MsgNum + "'";

                client.Logger.LogInformation(msgHead);
            }
            else if (version == FinTsVersion.v300)
            {
                sb = new StringBuilder();
                sb.Append("HNHBK");
                sb.Append(DEG.Separator);
                sb.Append("1");
                sb.Append(DEG.Separator);
                sb.Append("3");
                sb.Append(sEG.Delimiter);
                sb.Append(paddedLen);
                sb.Append(sEG.Delimiter);
                sb.Append((int) FinTsVersion.v300);
                sb.Append(sEG.Delimiter);
                sb.Append(DialogID);
                sb.Append(sEG.Delimiter);
                sb.Append(MsgNum);
                sb.Append(sEG.Terminator);
                msgHead = sb.ToString();
                //msgHead = "HNHBK:1:3+" + paddedLen + "+" + (HBCI.v300) + "+" + DialogID + "+" + MsgNum + "'";

                client.Logger.LogInformation(msgHead);
            }
            else
            {
                throw new FinTsVersionNotSupportedException(version,
                    new[] { FinTsVersion.v220, FinTsVersion.v300 });
            }

            sb = new StringBuilder();
            sb.Append("HNHBS");
            sb.Append(DEG.Separator);
            sb.Append(Convert.ToString(SegmentNum + 2));
            sb.Append(DEG.Separator);
            sb.Append("1");
            sb.Append(sEG.Delimiter);
            sb.Append(MsgNum);
            sb.Append(sEG.Terminator);
            var msgEnd = sb.ToString();
            // var msgEnd = "HNHBS:" + Convert.ToString(SegmentNum + 2) + ":1+" + MsgNum + "'";

            client.Logger.LogInformation(msgEnd);

            //UserID = string.Empty;
            //PIN = null;

            return msgHead + encHead + payload + msgEnd;
        }

        private static void TraceUserTan(FinTsClient client, string message, string userId, string pin)
        {
            message = Regex.Replace(message, $@"\b{Regex.Escape(userId)}", new string('X', userId.Length));
            message = Regex.Replace(message, $@"\b{Regex.Escape(pin)}", new string('X', pin.Length));

            if (client.FormattedTrace)
            {
                var formatted = string.Empty;
                var matches = Regex.Matches(message, "[A-Z]+?[^']*'+");
                foreach (Match match in matches)
                {
                    formatted += match.Value + Environment.NewLine;
                }
                client.Logger.LogTrace(formatted);
            }
            else
            {
                client.Logger.LogTrace(message);
            }
        }

        /// <summary>
        /// Send FinTS message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        public static async Task<string> Send(FinTsClient client, string Message)
        {
            client.Logger.LogInformation("Connect to FinTS Server");
            client.Logger.LogInformation("Url: " + client.ConnectionDetails.Url);

            TraceUserTan(client, Message, client.ConnectionDetails.UserIdEscaped, client.ConnectionDetails.Pin);

            return await SendAsync(client, Message);
        }

        /// <summary>
        /// Send FinTS message async
        /// </summary>
        /// <param name="client"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        private static async Task<string> SendAsync(FinTsClient client, string Message)
        {
            try
            {
                string FinTSMessage = string.Empty;
                ServicePointManager.SecurityProtocol = client.ConnectionDetails.SecurityProtocol;
                var req = WebRequest.Create(client.ConnectionDetails.Url) as HttpWebRequest;

                byte[] data = Encoding.ASCII.GetBytes(Helper.EncodeTo64(Message));

                req.Method = "POST";
                req.Timeout = 10000;
                req.ContentType = "application/octet-stream";
                req.ContentLength = data.Length;
                req.KeepAlive = false;

                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(data, 0, data.Length);
                    await reqStream.FlushAsync();
                }

                using (var res = (HttpWebResponse) await req.GetResponseAsync())
                {
                    using (var resStream = res.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(resStream, Encoding.UTF8))
                        {
                            FinTSMessage = Helper.DecodeFrom64EncodingDefault(streamReader.ReadToEnd());
                        }
                    }
                }

                TraceUserTan(client, Message, client.ConnectionDetails.UserIdEscaped, client.ConnectionDetails.Pin);

                return FinTSMessage;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Fehler beim Versenden der HBCI-Nachricht.", ex);
            }
        }
    }
}
