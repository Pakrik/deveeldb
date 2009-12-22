//  
//  DeveelDbRowUpdatedEventArgs.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
// 
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Data;
using System.Data.Common;

namespace Deveel.Data.Client {
	public delegate void DeveelDbRowUpdatedEventHandler(object sender, DeveelDbRowUpdatedEventArgs e);

	public sealed class DeveelDbRowUpdatedEventArgs : RowUpdatedEventArgs {
		public DeveelDbRowUpdatedEventArgs(System.Data.DataRow dataRow, DeveelDbCommand command, StatementType statementType, DataTableMapping tableMapping) 
			: base(dataRow, command, statementType, tableMapping) {
		}

		public new DeveelDbCommand Command {
			get { return (DeveelDbCommand) base.Command; }
		}
	}
}