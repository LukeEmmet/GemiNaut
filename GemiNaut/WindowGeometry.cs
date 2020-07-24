//===================================================
//    GemiNaut, a friendly browser for Gemini space on Windows

//    Copyright (C) 2020, Luke Emmet 

//    Email: luke [dot] emmet [at] gmail [dot] com

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.
//===================================================

using System;
using System.Windows;

namespace GemiNaut
{
    static class WindowGeometry
    {

        //get the position of the centre of a window
        public static Tuple<int, int> WindowCentre(Window window)
        {
            var inputLeft = (int)(window.Left + (window.Width / 2) - 180);
            var inputTop = (int)(window.Top + (window.Height / 2) - 20);

            return new Tuple<int, int>(inputLeft, inputTop);
        }
    }
}
