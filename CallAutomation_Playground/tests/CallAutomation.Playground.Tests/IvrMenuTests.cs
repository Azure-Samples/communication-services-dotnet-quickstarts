using AutoFixture;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using CallAutomation.Playground.Exceptions;
using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;
using CallAutomation.Playground.Tones;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CallAutomation.Playground.Tests
{
    public class IvrMenuTests
    {
        [Fact]
        public async Task IvrMenuBuilder_Builds_ThrowsCorrectly()
        {
            // arrange
            var mockCallConnectionProperties = CallAutomationModelFactory.CallConnectionProperties("abc");
            var mockCallAutomationClient = new Mock<CallAutomationClient>();
            
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(mockCallAutomationClient.Object);
            services.AddSingleton<ICallingServices, CallingServices>();

            services.AddIvrMenu("Test", x =>
            {
                x.AddChoice<One, TestChoice>().Build();
            });

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var ivrRegistry = serviceProvider.GetService<IvrMenuRegistry>();

            // act
            var menu = ivrRegistry.IvrMenus["Test"];

            // assert
            menu.Should().NotBeNull();
            await menu.Invoking(x => x.OnPress<One>(default, mockCallConnectionProperties, CommunicationIdentifier.FromRawId("8:acs:abc_123"), CancellationToken.None)).Should().ThrowAsync<NotImplementedException>();
        }

        [Fact]
        public async Task IvrMenuBuilder_BuildsWrongTone_Executes()
        {
            // arrange
            var mockCallConnectionProperties = CallAutomationModelFactory.CallConnectionProperties("abc");
            var mockCallAutomationClient = new Mock<CallAutomationClient>();
            var mockCallingServices = new Mock<ICallingServices>();
            mockCallingServices.Setup(x => x.PlayAudio(mockCallConnectionProperties, It.IsAny<Uri>(), null, false, CancellationToken.None)).Returns(Task.CompletedTask);

            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(mockCallAutomationClient.Object);
            services.AddSingleton(mockCallingServices.Object);

            services.AddIvrMenu("Test", x =>
            {
                x.WithConfiguration(y => y.InvalidEntryUri = new Uri("https://something.com"))
                    .AddChoice<One, TestChoice>()
                    .Build();
            });

            var serviceProvider = services.BuildServiceProvider();
            var ivrRegistry = serviceProvider.GetRequiredService<IvrMenuRegistry>();

            // act
            var menu = ivrRegistry.IvrMenus["Test"];

            // assert
            menu.Should().NotBeNull();
            await menu.Invoking(x => x.OnPress<Two>(default, mockCallConnectionProperties, CommunicationIdentifier.FromRawId("8:acs:abc_123"), CancellationToken.None)).Should().ThrowAsync<InvalidEntryException>();
        }
    }
}