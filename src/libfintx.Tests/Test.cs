﻿/*	
 * 	
 *  This file is part of libfintx.
 *  
 *  Copyright (c) 2016 - 2020 Torsten Klinger
 * 	E-Mail: torsten.klinger@googlemail.com
 * 	
 * 	libfintx is free software; you can redistribute it and/or
 *	modify it under the terms of the GNU Lesser General Public
 * 	License as published by the Free Software Foundation; either
 * 	version 2.1 of the License, or (at your option) any later version.
 *	
 * 	libfintx is distributed in the hope that it will be useful,
 * 	but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * 	Lesser General Public License for more details.
 *	
 * 	You should have received a copy of the GNU Lesser General Public
 * 	License along with libfintx; if not, write to the Free Software
 * 	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 * 	
 */

//#define WINDOWS

using libfintx.FinTS.Data;
using System;
using System.Linq;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using libfintx.FinTS;

#if (DEBUG && WINDOWS)
using hbci = libfintx;

using System.Windows.Forms;
#endif

#if USE_LIB_SixLabors_ImageSharp
using SixLabors.ImageSharp;
#endif

namespace libfintx.Tests;

public class Test
{
    private readonly ITestOutputHelper output;

    public Test(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact(Skip = "You have to provide the connection details for this test")]
    public async void Test_Balance()
    {
        var connectionDetails = new ConnectionDetails()
        {
            Account = "xxx",
            Blz = 76061482,
            Bic = "GENODEF1HSB",
            Iban = "xxx",
            Url = "https://fints2.atruvia.de/cgi-bin/hbciservlet",
            HbciVersion = 300,
            UserId = "xxx",
            Pin = "xxx"
        };

        var client = TestHelper.CreateTestClient(connectionDetails);

        #region Balance

        /* Balance */

        var balance = await client.Balance(new TANDialog(WaitForTanAsync));

        Console.WriteLine("[ Balance ]");
        Console.WriteLine();
        Console.WriteLine(balance.Data.Balance);
        Console.WriteLine();

        #endregion

        Console.ReadLine();
    }

    [Fact(Skip = "You have to provide the connection details for this test")]
    public async void Test_Accounts()
    {
        var connectionDetails = new ConnectionDetails()
        {
            // ...
        };

        /* Sync */
        var client = TestHelper.CreateTestClient(connectionDetails);

        var accounts = await client.Accounts(new TANDialog(WaitForTanAsync));
        foreach (var acc in accounts.Data)
        {
            output.WriteLine(acc.ToString());
        }
    }

    [Fact(Skip = "You have to provide the connection details for this test")]
    public async void Test_Request_TANMediumName()
    {
        var connectionDetails = new ConnectionDetails()
        {
            Blz = 76050101,
            Url = "https://banking-by1.s-fints-pt-by.de/fints30",
            HbciVersion = 300,
            UserId = "xxx",
            Pin = "xxx"
        };

        var client = TestHelper.CreateTestClient(connectionDetails);

        #region TanMediumName

        /* TANMediumname */

        var tanmediumname = await client.RequestTANMediumName();

        var t = tanmediumname.Data?.FirstOrDefault();

        Console.WriteLine("[ TAN Medium Name ]");
        Console.WriteLine();
        Console.WriteLine(t);
        Console.WriteLine();

        #endregion

        Console.ReadLine();
    }

    [Fact(Skip = "matrixcode.txt file is missing")]
    public void Test_PhotoTAN()
    {
        var PhotoCode = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\assets\\matrixcode.txt");

        var mCode = new MatrixCode(PhotoCode);

        Assert.NotNull(mCode.ImageMimeType);
        Assert.NotNull(mCode.ImageData);

#if USE_LIB_SixLabors_ImageSharp
            mCode.CodeImage.SaveAsPng(File.OpenWrite(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "matrixcode.png")));
#endif
    }

    [Fact(Skip = "You have to provide the connection details for this test")]
    public async void Test_PushTAN()
    {
        string receiver = string.Empty;
        string receiverIBAN = string.Empty;
        string receiverBIC = string.Empty;
        decimal amount = 0;
        string usage = string.Empty;

        ConnectionDetails connectionDetails = new ConnectionDetails()
        {
            AccountHolder = "Torsten Klinger",
            Blz = 76050101,
            Bic = "SSKNDE77XXX",
            Iban = "xxx",
            Url = "https://banking-by1.s-fints-pt-by.de/fints30",
            HbciVersion = 300,
            UserId = "xxx",
            Pin = "xxx"
        };

        var client = TestHelper.CreateTestClient(connectionDetails);

        receiver = "Klinger";
        receiverIBAN = "xxx";
        receiverBIC = "GENODEF1HSB";
        amount = 1.0m;
        usage = "TEST";

        var result = await client.Synchronization();

        if (result.IsSuccess)
        {
            string hirms = "921"; // -> pushTAN

            var tanmediumname = await client.RequestTANMediumName();
            client.HITAB = tanmediumname.Data.FirstOrDefault();

            Console.WriteLine(client.Transfer(new TANDialog(WaitForTanAsync), receiver, receiverIBAN, receiverBIC, amount, usage, hirms));
        }
    }

    public async Task<string> WaitForTanAsync(TANDialog tanDialog)
    {
        foreach (var msg in tanDialog.DialogResult.Messages)
            Console.WriteLine(msg);

        return Console.ReadLine();
    }
}
