﻿// 
//  Copyright 2010-2015 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//


using System;
using System.Collections.Generic;

using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Sql.Triggers {
	/// <summary>
	/// Defines the information about a trigger on a table of the
	/// database, such as the event on which is fired and the
	/// procedure to execute.
	/// </summary>
	public class TriggerInfo : IObjectInfo {
		/// <summary>
		/// Constructs a new callback trigger information object with the given name,
		/// and the event at which it should be fired.
		/// </summary>
		/// <param name="triggerName">The fully qualified name of the trigger.</param>
		/// <param name="eventType">The modification event at which to fire the trigger.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="triggerName"/> is <c>null</c>
		/// </exception>
		public TriggerInfo(ObjectName triggerName, TriggerEventType eventType) 
			: this(triggerName, TriggerType.Callback, eventType, null) {
		}

		/// <summary>
		/// Constructs a new trigger information object with the given name,
		/// the name of the table on which it is attached and the event
		/// at which it should be fired.
		/// </summary>
		/// <param name="triggerName">The fully qualified name of the trigger.</param>
		/// <param name="eventType">The modification event on the given table at which to 
		/// fire the trigger.</param>
		/// <param name="triggerType">The type of trigger.</param>
		/// <param name="tableName">The fully qualified name of the table on which to attach
		/// the trigger.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="triggerName"/> is <c>null</c>
		/// </exception>
		public TriggerInfo(ObjectName triggerName, TriggerType triggerType, TriggerEventType eventType, ObjectName tableName) {
			if (triggerName == null)
				throw new ArgumentNullException("triggerName");

			if (triggerType == TriggerType.Callback &&
				tableName != null)
				throw new ArgumentException("A CALLBACK TRIGGER cannot define any table to be attached to.");

			TriggerName = triggerName;
			EventType = eventType;
			TableName = tableName;
			TriggerType = triggerType;

			Arguments = new List<SqlExpression>();

			Body = new TriggerBody(this);
		}

		/// <summary>
		/// Gets the fully qualified name of the trigger.
		/// </summary>
		public ObjectName TriggerName { get; private set; }

		/// <summary>
		/// Gets the modification event on the attached table at 
		/// which to fire the trigger.
		/// </summary>
		public TriggerEventType EventType { get; private set; }

		/// <summary>
		/// Gets the type of trigger.
		/// </summary>
		/// <seealso cref="TriggerType"/>
		public TriggerType TriggerType { get; private set; }

		/// <summary>
		/// Gets the fully qualified name of the database table on which to
		/// attach the trigger.
		/// </summary>
		public ObjectName TableName { get; private set; }

		/// <summary>
		/// Gets the procedural body of the trigger to be executed.
		/// </summary>
		public TriggerBody Body { get; private set; }

		/// <summary>
		/// Gets or sets the name of a stored procedure to be executed
		/// when the trigger is fired.
		/// </summary>
		public ObjectName ProcedureName { get; set; }

		public Type ExternalType { get; set; }

		public string ExternalMethod { get; set; }

		public ICollection<SqlExpression> Arguments { get; private set; }
			
		ObjectName IObjectInfo.FullName {
			get { return TriggerName; }
		}

		DbObjectType IObjectInfo.ObjectType {
			get { return DbObjectType.Trigger; }
		}
	}
}
