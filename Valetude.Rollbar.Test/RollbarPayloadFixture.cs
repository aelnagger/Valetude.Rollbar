﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Newtonsoft.Json.Linq;
using Valetude.Rollbar;
using Xunit;

namespace Rollbar.Test {
    public class RollbarPayloadFixture {
        private readonly RollbarPayload _exceptionExample;
        private readonly RollbarPayload _messageException;
        private readonly RollbarPayload _crashException;
        private readonly RollbarPayload _aggregateExample;

        public RollbarPayloadFixture() {
            this._exceptionExample = new RollbarPayload("access-token", new RollbarData("test", new RollbarBody(GetException())));
            this._messageException = new RollbarPayload("access-token", new RollbarData("test", new RollbarBody(new RollbarMessage("A message I wish to send to the rollbar overlords"))));
            this._crashException = new RollbarPayload("access-token", new RollbarData("test", new RollbarBody("A terrible crash!")));
            this._aggregateExample = new RollbarPayload("access-token", new RollbarData("test", new RollbarBody(GetAggregateException())));
        }

        // The following three methods make it easier to test the RollbarFrame becuase the fix the location
        // of the error to an easy to type ("ThrowAnException") and constant location.
        // Figuring out how to write .ctor_bFunc_Method0143 was kind of a bummer. So I didn't.
        private static void ThrowAnException() {
            throw new Exception("Test");
        }

        private static Exception GetException() {
            try {
                ThrowAnException();
                throw new Exception("Unreachable");
            }
            catch (Exception e) {
                return e;
            }
        }

        private static AggregateException GetAggregateException() {
            try {
                Parallel.ForEach(new[] {1, 2}, i => ThrowAnException());
            }
            catch (AggregateException e) {
                return e;
            }
            throw new Exception("Unreachable");
        }

        [Fact]
        public void Basic_exception_creates_valid_rollbar_object() {
            var asJson = JObject.Parse(_exceptionExample.ToJson());

            Assert.Equal("access-token", asJson["access_token"].Value<string>());
            var data = Assert.IsType<JObject>(asJson["data"]);
            Assert.Equal("test", data["environment"].Value<string>());
            var body = Assert.IsType<JObject>(data["body"]);
            var trace = Assert.IsType<JObject>(body["trace"]);
            Assert.Null(body["trace_chain"]);
            Assert.Null(body["message"]);
            Assert.Null(body["crash_report"]);

            var frames = Assert.IsType<JArray>(trace["frames"]);
            Assert.All(frames, token => Assert.NotNull(token["filename"]));
            Assert.Equal("Rollbar.Test.RollbarPayloadFixture.ThrowAnException()", frames[0]["method"].Value<string>());
            var exception = Assert.IsType<JObject>(trace["exception"]);
            Assert.Equal("Test", exception["message"].Value<string>());
            Assert.Equal("System.Exception", exception["class"].Value<string>());

            Assert.Equal("windows", data["platform"].Value<string>());
            Assert.Equal("c#", data["language"].Value<string>());
            Assert.Equal(_exceptionExample.RollbarData.Notifier.ToArray(), data["notifier"].ToObject<Dictionary<string, string>>());

            IEnumerable<string> keys = data.Properties().Select(p => p.Name).ToArray();

            Assert.False(keys.Contains("level"), "level should not be present");
            Assert.False(keys.Contains("code_version"), "code_version should not be present");
            Assert.False(keys.Contains("framework"), "framework should not be present");
            Assert.False(keys.Contains("context"), "context should not be present");
            Assert.False(keys.Contains("request"), "request should not be present");
            Assert.False(keys.Contains("person"), "person should not be present");
            Assert.False(keys.Contains("server"), "server should not be present");
            Assert.False(keys.Contains("client"), "client should not be present");
            Assert.False(keys.Contains("custom"), "custom should not be present");
            Assert.False(keys.Contains("fingerprint"), "fingerprint should not be present");
            Assert.False(keys.Contains("title"), "title should not be present");
            Assert.False(keys.Contains("uuid"), "uuid should not be present");
        }

        [Fact]
        public void Message_creates_valid_rollbar_object() {
            var asJson = JObject.Parse(_messageException.ToJson());

            Assert.Equal("access-token", asJson["access_token"].Value<string>());
            var data = Assert.IsType<JObject>(asJson["data"]);
            Assert.Equal("test", data["environment"].Value<string>());
            var body = Assert.IsType<JObject>(data["body"]);
            var message = Assert.IsType<JObject>(body["message"]);
            Assert.Null(body["trace_chain"]);
            Assert.Null(body["trace"]);
            Assert.Null(body["crash_report"]);

            Assert.Equal("A message I wish to send to the rollbar overlords", message["body"].Value<string>());
            Assert.Single(message.Properties());

            Assert.Equal("windows", data["platform"].Value<string>());
            Assert.Equal("c#", data["language"].Value<string>());
            Assert.Equal(_exceptionExample.RollbarData.Notifier.ToArray(), data["notifier"].ToObject<Dictionary<string, string>>());

            IEnumerable<string> keys = data.Properties().Select(p => p.Name).ToArray();

            Assert.False(keys.Contains("level"), "level should not be present");
            Assert.False(keys.Contains("code_version"), "code_version should not be present");
            Assert.False(keys.Contains("framework"), "framework should not be present");
            Assert.False(keys.Contains("context"), "context should not be present");
            Assert.False(keys.Contains("request"), "request should not be present");
            Assert.False(keys.Contains("person"), "person should not be present");
            Assert.False(keys.Contains("server"), "server should not be present");
            Assert.False(keys.Contains("client"), "client should not be present");
            Assert.False(keys.Contains("custom"), "custom should not be present");
            Assert.False(keys.Contains("fingerprint"), "fingerprint should not be present");
            Assert.False(keys.Contains("title"), "title should not be present");
            Assert.False(keys.Contains("uuid"), "uuid should not be present");
        }

        [Fact]
        public void Trace_chain_creates_a_valid_rollbar_object() {
            var asJson = JObject.Parse(_aggregateExample.ToJson());

            Assert.Equal("access-token", asJson["access_token"].Value<string>());
            var data = Assert.IsType<JObject>(asJson["data"]);
            Assert.Equal("test", data["environment"].Value<string>());
            var body = Assert.IsType<JObject>(data["body"]);
            var traceChain = Assert.IsType<JArray>(body["trace_chain"]).ToArray();
            Assert.Null(body["trace"]);
            Assert.Null(body["message"]);
            Assert.Null(body["crash_report"]);

            Assert.All(traceChain, trace => {
                var frames = Assert.IsType<JArray>(trace["frames"]);
                Assert.All(frames, frame => Assert.NotNull(frame["filename"]));
                Assert.Equal("Rollbar.Test.RollbarPayloadFixture.ThrowAnException()", frames[0]["method"].Value<string>());
                var exception = Assert.IsType<JObject>(trace["exception"]);
                Assert.Equal("Test", exception["message"].Value<string>());
                Assert.Equal("System.Exception", exception["class"].Value<string>());
            });

            Assert.Equal("windows", data["platform"].Value<string>());
            Assert.Equal("c#", data["language"].Value<string>());
            Assert.Equal(_exceptionExample.RollbarData.Notifier.ToArray(), data["notifier"].ToObject<Dictionary<string, string>>());

            IEnumerable<string> keys = data.Properties().Select(p => p.Name).ToArray();

            Assert.False(keys.Contains("level"), "level should not be present");
            Assert.False(keys.Contains("code_version"), "code_version should not be present");
            Assert.False(keys.Contains("framework"), "framework should not be present");
            Assert.False(keys.Contains("context"), "context should not be present");
            Assert.False(keys.Contains("request"), "request should not be present");
            Assert.False(keys.Contains("person"), "person should not be present");
            Assert.False(keys.Contains("server"), "server should not be present");
            Assert.False(keys.Contains("client"), "client should not be present");
            Assert.False(keys.Contains("custom"), "custom should not be present");
            Assert.False(keys.Contains("fingerprint"), "fingerprint should not be present");
            Assert.False(keys.Contains("title"), "title should not be present");
            Assert.False(keys.Contains("uuid"), "uuid should not be present");
        }

        [Fact]
        public void Crash_report_creates_a_valid_rollbar_object() {
            var asJson = JObject.Parse(_crashException.ToJson());

            Assert.Equal("access-token", asJson["access_token"].Value<string>());
            var data = Assert.IsType<JObject>(asJson["data"]);
            Assert.Equal("test", data["environment"].Value<string>());
            var body = Assert.IsType<JObject>(data["body"]);
            var crashReport = Assert.IsType<JObject>(body["crash_report"]);
            Assert.Null(body["trace_chain"]);
            Assert.Null(body["message"]);
            Assert.Null(body["trace"]);

            Assert.Equal("A terrible crash!", crashReport["raw"].Value<string>());

            Assert.Equal("windows", data["platform"].Value<string>());
            Assert.Equal("c#", data["language"].Value<string>());
            Assert.Equal(_exceptionExample.RollbarData.Notifier.ToArray(), data["notifier"].ToObject<Dictionary<string, string>>());

            IEnumerable<string> keys = data.Properties().Select(p => p.Name).ToArray();

            Assert.False(keys.Contains("level"), "level should not be present");
            Assert.False(keys.Contains("code_version"), "code_version should not be present");
            Assert.False(keys.Contains("framework"), "framework should not be present");
            Assert.False(keys.Contains("context"), "context should not be present");
            Assert.False(keys.Contains("request"), "request should not be present");
            Assert.False(keys.Contains("person"), "person should not be present");
            Assert.False(keys.Contains("server"), "server should not be present");
            Assert.False(keys.Contains("client"), "client should not be present");
            Assert.False(keys.Contains("custom"), "custom should not be present");
            Assert.False(keys.Contains("fingerprint"), "fingerprint should not be present");
            Assert.False(keys.Contains("title"), "title should not be present");
            Assert.False(keys.Contains("uuid"), "uuid should not be present");
        }

        [Fact]
        public void RollbarPayload_cannot_have_null_access_token() {
            Assert.Throws<ArgumentNullException>(() => {
                var x = new RollbarPayload(null, A<RollbarData>.Ignored);
            });
        }

        [Fact]
        public void RollbarPayload_cannot_have_null_data() {
            Assert.Throws<ArgumentNullException>(() => {
                var x = new RollbarPayload("test", null);
            });
        }
    }
}
