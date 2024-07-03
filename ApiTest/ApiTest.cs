using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;
namespace ApiTest
{
    public class ApiTests
    {
        private readonly HttpClient _httpClient;
        private readonly TestObject testObject = new() { name = "Test Name", data = "Test Data" };
        private readonly TestObject updatingObject = new() { name = "Updated Name", data = "Updated Data" };
        private readonly string exceptionMessage = "*The JSON value could not be converted to System.Collections.Generic.List`1[ApiTest.TestObject]*";
        public ApiTests()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.restful-api.dev/")
            };
        }

        [Fact]
        public async Task GetAllObjects_ShouldReturnListOfObjects_Success()
        {
            var response = await _httpClient.GetAsync("objects");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var objects = JsonSerializer.Deserialize<List<TestObject>>(content);

            objects.Should().NotBeNull();
            objects.Should().BeOfType<List<TestObject>>();
        }

        [Fact]
        public async Task AddObject_ShouldCreateNewObject_Success()
        {
            var newObject = new TestObject { name = testObject.name, data = testObject.data };
            var content = new StringContent(JsonSerializer.Serialize(newObject), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("objects", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdObject = JsonSerializer.Deserialize<TestObject>(responseContent);

            createdObject.Should().NotBeNull();
            createdObject.name.Should().Be(newObject.name);
            createdObject.data.ToString().Should().Be(newObject.data.ToString());
        }

        [Fact]
        public async Task GetObjectById_ShouldReturnSingleObject_Success()
        {
            var newObject = new TestObject { name = testObject.name, data = testObject.data };
            var content = new StringContent(JsonSerializer.Serialize(newObject), Encoding.UTF8, "application/json");
            var postResponse = await _httpClient.PostAsync("objects", content);
            postResponse.EnsureSuccessStatusCode();
            var createdObject = JsonSerializer.Deserialize<TestObject>(await postResponse.Content.ReadAsStringAsync());

            var response = await _httpClient.GetAsync($"objects/{createdObject.id}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var retrievedObject = JsonSerializer.Deserialize<TestObject>(responseContent);

            retrievedObject.Should().NotBeNull();
            retrievedObject.id.Should().Be(createdObject.id);
            retrievedObject.name.Should().Be(createdObject.name);
            retrievedObject.data.ToString().Should().Be(createdObject.data.ToString());
        }

        [Fact]
        public async Task UpdateObject_ShouldModifyObject_Success()
        {
            var newObject = new TestObject { name = testObject.name, data = testObject.data };
            var content = new StringContent(JsonSerializer.Serialize(newObject), Encoding.UTF8, "application/json");
            var postResponse = await _httpClient.PostAsync("objects", content);
            postResponse.EnsureSuccessStatusCode();
            var createdObject = JsonSerializer.Deserialize<TestObject>(await postResponse.Content.ReadAsStringAsync());

            var updatedObject = new TestObject { id = createdObject.id, name = updatingObject.name, data = updatingObject.data };
            var updateContent = new StringContent(JsonSerializer.Serialize(updatedObject), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"objects/{createdObject.id}", updateContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var modifiedObject = JsonSerializer.Deserialize<TestObject>(responseContent);

            modifiedObject.Should().NotBeNull();
            modifiedObject.id.Should().Be(createdObject.id);
            modifiedObject.name.Should().Be(updatedObject.name);
            modifiedObject.data.ToString().Should().Be(updatedObject.data.ToString());
        }

        [Fact]
        public async Task DeleteObject_ShouldRemoveObject_Success()
        {
            var newObject = new TestObject { name = testObject.name, data = testObject.data };
            var content = new StringContent(JsonSerializer.Serialize(newObject), Encoding.UTF8, "application/json");
            var postResponse = await _httpClient.PostAsync("objects", content);
            postResponse.EnsureSuccessStatusCode();
            var createdObject = JsonSerializer.Deserialize<TestObject>(await postResponse.Content.ReadAsStringAsync());

            var response = await _httpClient.DeleteAsync($"objects/{createdObject.id}");
            response.EnsureSuccessStatusCode();

            var getResponse = await _httpClient.GetAsync($"objects/{createdObject.id}");

            getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        [Fact]
        public async Task GetAllObjects_ShouldReturnListOfObjects_Failure()
        {
            var invalidJson = @"{ 'invalid': 'json' }";
            var stringContent = new StringContent(invalidJson, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(_httpClient.BaseAddress + "objects")
                   .Respond(HttpStatusCode.OK, stringContent);

            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = _httpClient.BaseAddress;
            var response = await httpClient.GetAsync("objects");

            response.IsSuccessStatusCode.Should().BeTrue();

            var content = await response.Content.ReadAsStringAsync();
            Action deserializeAction = () => JsonSerializer.Deserialize<List<TestObject>>(content);

            deserializeAction.Should().Throw<JsonException>().WithMessage(exceptionMessage);
        }

    }

}