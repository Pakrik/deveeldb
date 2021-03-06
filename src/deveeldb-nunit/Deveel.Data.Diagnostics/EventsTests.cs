﻿// 
//  Copyright 2010-2014 Deveel
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

using System;
using System.Threading;

using NUnit.Framework;

namespace Deveel.Data.Diagnostics {
	[TestFixture]
	public class EventsTests : ContextBasedTest {
		protected override void OnBeforeTearDown(string testName) {
			if (testName != "RouteError")
				base.OnBeforeTearDown(testName);
		}

		[Test]
		public void AttachRouter() {
			IEvent firedEvent = null;
			Assert.DoesNotThrow(() => AdminQuery.Context.Route<ErrorEvent>(@event => firedEvent = @event));
			Assert.IsNull(firedEvent);
		}

		[Test]
		public void RouteError() {
			var reset = new AutoResetEvent(false);

			IEvent firedEvent = null;
			AdminQuery.Context.Route<ErrorEvent>(e => {
				firedEvent = e;
				reset.Set();
			});

			AdminQuery.OnError(new Exception("Test Error"));

			reset.WaitOne();

			Assert.IsNotNull(firedEvent);
			Assert.IsInstanceOf<ErrorEvent>(firedEvent);
		}

		[Test]
		public void FireAtLowerLevelAndListenAtHighest() {
			var reset = new AutoResetEvent(false);

			IEvent fired = null;
			System.Context.Route<InformationEvent>(e => {
				fired = e;
				reset.Set();
			});

			AdminQuery.OnVerbose("Test Message");

			reset.WaitOne(300);

			Assert.IsNotNull(fired);
			Assert.IsInstanceOf<InformationEvent>(fired);

			var infoEvent = (InformationEvent) fired;
			Assert.AreEqual(InformationLevel.Verbose, infoEvent.Level);
			Assert.AreEqual("Test Message", infoEvent.Message);
		}

		[Test]
		public void RouteOnlyOnce() {
			var reset1 = new AutoResetEvent(false);
			var reset2 = new AutoResetEvent(false);

			IEvent systemFired = null;
			System.Context.Route<InformationEvent>(e => {
				systemFired = e;
				reset1.Set();
			});

			IEvent sessionFired = null;

			AdminSession.Context.Route<ErrorEvent>(e => {
				sessionFired = e;
				reset2.Set();
			});

			AdminQuery.OnVerbose("Test Message");

			reset1.WaitOne(300);
			reset2.WaitOne(300);

			Assert.IsNotNull(systemFired);
			Assert.IsNull(sessionFired);
		}

		[Test]
		public void RouteOnlyOnceForSameEventType() {
			var reset1 = new AutoResetEvent(false);
			var reset2 = new AutoResetEvent(false);

			IEvent systemFired = null;
			System.Context.Route<InformationEvent>(e => {
				systemFired = e;
				reset1.Set();
			});

			IEvent sessionFired = null;

			AdminSession.Context.Route<InformationEvent>(e => {
				sessionFired = e;
				reset2.Set();
			}, e => e.Level == InformationLevel.Debug);

			AdminQuery.OnVerbose("Test Message");

			reset1.WaitOne(300);
			reset2.WaitOne(300);

			Assert.IsNotNull(systemFired);
			Assert.IsNull(sessionFired);
		}

		[Test]
		public void RouteTwiceForSameEventType() {
			var reset1 = new AutoResetEvent(false);
			var reset2 = new AutoResetEvent(false);

			IEvent systemFired = null;
			System.Context.Route<InformationEvent>(e => {
				systemFired = e;
				reset1.Set();
			});

			IEvent sessionFired = null;

			AdminSession.Context.Route<InformationEvent>(e => {
				sessionFired = e;
				reset2.Set();
			});

			AdminQuery.OnVerbose("Test Message");

			reset1.WaitOne(300);
			reset2.WaitOne(300);

			Assert.IsNotNull(systemFired);
			Assert.IsNotNull(sessionFired);
		}

		[Test]
		public void RouteOneRegisteredMany() {
			var reset = new AutoResetEvent(false);

			QueryEvent a = null, b = null;
			System.Context.Route<QueryEvent>(e => a = e);
			System.Context.Route<QueryEvent>(e => b = e);

			IEvent fired = null;
			System.Context.Route<InformationEvent>(e => {
				fired = e;
				reset.Set();
			});

			AdminQuery.OnVerbose("Test Message");

			reset.WaitOne(300);

			Assert.IsNotNull(fired);
			Assert.IsInstanceOf<InformationEvent>(fired);

			Assert.IsNull(a);
			Assert.IsNull(b);

			var infoEvent = (InformationEvent) fired;
			Assert.AreEqual(InformationLevel.Verbose, infoEvent.Level);
			Assert.AreEqual("Test Message", infoEvent.Message);
		}

		[Test]
		public void GetEventData_ConvertEnum() {
			var e = new InformationEvent("test", InformationLevel.Debug);

			var level = e.GetData<InformationLevel>(MetadataKeys.Event.Information.Level);
			var message = e.GetData<string>(MetadataKeys.Event.Information.Message);

			Assert.IsNotNull(message);
			Assert.AreSame("test", message);
			Assert.AreEqual(InformationLevel.Debug, level);
		}

		[Test]
		public void FormMessageAndReadMeta() {
			var reset = new AutoResetEvent(false);

			Event info = null;
			System.Context.RouteImmediate<InformationEvent>(e => {
				info = e;
				reset.Set();
			}, e => e.Level == InformationLevel.Verbose);

			AdminQuery.OnVerbose("Testing Messages");

			reset.WaitOne(300);

			Assert.IsNotNull(info);
			Assert.IsInstanceOf<InformationEvent>(info);

			var message = info.AsMessage();
			Assert.IsNotNull(message);

			var databaseName = message.DatabaseName();
			Assert.IsNotNullOrEmpty(databaseName);
			Assert.AreEqual(DatabaseName, databaseName);
		}
	}
}
