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

#if USE_LIB_SixLabors_ImageSharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif
using System;
using System.Threading.Tasks;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace libfintx.FinTS
{
    /// <summary>
    /// Helper object needed for entering a TAN.
    /// </summary>
    public class TANDialog
    {
#if WINDOWS
        public PictureBox PictureBox { get; }
#else
        public object PictureBox { get; }
#endif

        public bool RenderFlickerCodeAsGif { get; }

        public int FlickerWidth { get; }

        public int FlickerHeight { get; }

        public HBCIDialogResult DialogResult { get; internal set; }

#if USE_LIB_SixLabors_ImageSharp
        public Image<Rgba32> FlickerImage { get; internal set; }

        public Image<Rgba32> MatrixImage => MatrixCode.CodeImage;
#endif

        public MatrixCode MatrixCode { get; internal set; }

        /// <summary>
        /// Bei Verwendung des Decoupled-Verfahren (HKTAN#7) setzen.
        /// </summary>
        public bool IsDecoupled { get; set; }

        /// <summary>
        /// HKTAN#7: Kann gesetzt werden, um die automatisierte Statusabfrage für eine Freigabe zu unterbrechen.
        /// </summary>
        public bool IsCancelWaitForApproval { get; set; }

        /// <summary>
        /// Der Aufrufer kann sich hier registrieren, um darüber benachrichtigt zu werden, dass die Statusabfrage erteilt wurde.
        /// </summary>
        private readonly Func<bool, Task> _onTransactionEndAsync;

        private readonly Func<TANDialog, Task<string>> _waitForTanAsync;

        /// <summary>
        /// Render Flickercode as GIF.
        /// </summary>
        /// <param name="waitForTanAsync"></param>
        /// <param name="dialogResult"></param>
        /// <param name="flickerWidth"></param>
        /// <param name="flickerHeight"></param>
#if !USE_LIB_SixLabors_ImageSharp
        [Obsolete("This constructor cannot be used, because the libfintx.FinTS library has been compiled without library support for SixLabors.ImageSharp.", true)]
#endif
        public TANDialog(Func<TANDialog, Task<string>> waitForTanAsync, int flickerWidth = 320, int flickerHeight = 120)
            : this(waitForTanAsync)
        {
#if USE_LIB_SixLabors_ImageSharp
            RenderFlickerCodeAsGif = true;
#else
            throw new NotSupportedException("This constructor cannot be used, because the libfintx.FinTS library has been compiled without library support for SixLabors.ImageSharp.");
#endif
            FlickerWidth = flickerWidth;
            FlickerHeight = flickerHeight;
        }

        /// <summary>
        /// Render TANCode (Flicker/Matrix) in WinForms.
        /// </summary>
        /// <param name="waitForTanAsync"></param>
        /// <param name="dialogResult"></param>
        /// <param name="pictureBox"></param>
        public TANDialog(Func<TANDialog, Task<string>> waitForTanAsync, object pictureBox)
            : this(waitForTanAsync)
        {
            PictureBox = pictureBox;
        }

        /// <summary>
        /// Enter a TAN without any visual components, e.g. pushTAN or mobileTAN.
        /// </summary>
        /// <param name="waitForTanAsync">Function which takes a </param>
        /// <param name="dialogResult"></param>
        /// <param name="matrixImage"></param>
        public TANDialog(Func<TANDialog, Task<string>> waitForTanAsync)
        {
            _waitForTanAsync = waitForTanAsync;
        }

        /// <summary>
        /// Enter a TAN without any visual components, e.g. pushTAN or mobileTAN.
        /// </summary>
        /// <param name="waitForTanAsync">Function which takes a </param>
        /// <param name="dialogResult"></param>
        /// <param name="matrixImage"></param>
        public TANDialog(Func<TANDialog, Task<string>> waitForTanAsync, Func<bool, Task> onTransactionEndAsync)
        {
            _waitForTanAsync = waitForTanAsync;
            _onTransactionEndAsync = onTransactionEndAsync;
        }

        /// <summary>
        /// Wait for the user to enter a TAN.
        /// </summary>
        /// <param name="dialogResult">The <code>HBCIDialogResult</code> from the bank which requests the TAN. Can be used to display bank messages in the dialog.</param>
        /// <returns></returns>
        internal async Task<string> WaitForTanAsync()
        {
            return await _waitForTanAsync.Invoke(this);
        }

        /// <summary>
        /// Wait for the user to enter a TAN.
        /// </summary>
        /// <param name="dialogResult">The <code>HBCIDialogResult</code> from the bank which requests the TAN. Can be used to display bank messages in the dialog.</param>
        /// <returns></returns>
        internal async Task OnTransactionEndAsync(bool success)
        {
            await (_onTransactionEndAsync != null ? _onTransactionEndAsync.Invoke(success) : Task.CompletedTask);
        }
    }
}
