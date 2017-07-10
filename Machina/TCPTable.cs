// Machina ~ TCPTable.cs
// 
// Copyright © 2007 - 2017 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections;
using System.Collections.Generic;

namespace Machina
{
    public class TCPTable : IEnumerable
    {
        #region Private Fields

        private IEnumerable<TCPRow> rows;

        #endregion

        #region Constructors

        public TCPTable(IEnumerable<TCPRow> rows)
        {
            this.rows = rows;
        }

        #endregion

        #region Public Properties

        public IEnumerable<TCPRow> Rows
        {
            get { return rows; }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        #endregion

        #region IEnumerable<TCPRow> Members

        public IEnumerator<TCPRow> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        #endregion
    }
}
