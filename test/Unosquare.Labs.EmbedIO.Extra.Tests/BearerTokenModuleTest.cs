﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.BearerToken;
using EmbedIO.Extra.Tests.TestObjects;
using EmbedIO.Markdown;
using NUnit.Framework;
using Swan.Formatters;

namespace EmbedIO.Extra.Tests
{
    [TestFixture]
    public class BearerTokenModuleTest : EndToEndFixtureBase
    {
        protected BasicAuthorizationServerProvider BasicProvider = new BasicAuthorizationServerProvider();

        protected override void OnSetUp()
        {
            Server
                .WithBearerToken("/", "0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9eyJjbGF", new BasicAuthorizationServerProvider())
                .WithModule(new MarkdownStaticModule("/", TestHelper.SetupStaticFolder()));
        }

        [Test]
        public void TestBasicAuthorizationServerProvider()
        {
            Assert.Throws<ArgumentNullException>(() => BasicProvider
                .ValidateClientAuthentication(new ValidateClientAuthenticationContext(null))
                .RunSynchronously());
        }

        [Test]
        public async Task GetInvalidToken()
        {
            var payload = System.Text.Encoding.UTF8.GetBytes("grant_type=nothing");

            using var req = new HttpRequestMessage(HttpMethod.Post, WebServerUrl + "token") { Content = new ByteArrayContent(payload) };
            using var res = await Client.SendAsync(req);
            Assert.AreEqual(res.StatusCode, HttpStatusCode.Unauthorized);
        }

        [Test]
        public async Task GetValidToken()
        {
            var payload = System.Text.Encoding.UTF8.GetBytes("grant_type=password&username=test&password=test");

            using var req = new HttpRequestMessage(HttpMethod.Post, WebServerUrl + "token") { Content = new ByteArrayContent(payload) };

            using var res = await Client.SendAsync(req);
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            var jsonString = await res.Content.ReadAsStringAsync();
            var json = Json.Deserialize<BearerToken.BearerToken>(jsonString);
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json.Token);
            Assert.IsNotEmpty(json.Username);
            var token = json.Token;

            var indexRequest = new HttpRequestMessage(HttpMethod.Post, WebServerUrl + "index.html");

            using (var indexResponse = await Client.SendAsync(indexRequest))
            {
                Assert.AreEqual(indexResponse.StatusCode, HttpStatusCode.Unauthorized);
            }

            indexRequest = new HttpRequestMessage(HttpMethod.Get, WebServerUrl + "index.html");
            indexRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using (var indexResponse = await Client.SendAsync(indexRequest))
            {
                Assert.AreEqual(indexResponse.StatusCode, HttpStatusCode.OK);
            }
        }
    }
}