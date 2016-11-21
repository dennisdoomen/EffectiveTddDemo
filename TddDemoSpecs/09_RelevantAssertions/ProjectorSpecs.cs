using System;
using Chill;
using FluentAssertions;
using LiquidProjections;
using LiquidProjections.ExampleHost.Events;
using Xunit;

namespace ExampleHost.TddDemoSpecs._09_RelevantAssertions
{
    namespace ProjectorSpecs
    {
        public class Given_a_projector_with_an_in_memory_event_source : GivenSubject<Projector>
        {
            protected EventMapBuilder<ProjectionContext> MapBuilder;

            public Given_a_projector_with_an_in_memory_event_source()
            {
                Given(() =>
                {
                    UseThe(new MemoryEventSource());
                    MapBuilder = new EventMapBuilder<ProjectionContext>();

                    Container.Set<IEventMapBuilder<ProjectionContext>>(MapBuilder, string.Empty);
                });
            }
        }

        public class When_event_handling_fails : Given_a_projector_with_an_in_memory_event_source
        {
            public When_event_handling_fails()
            {
                Given(() =>
                {
                    MapBuilder.Map<LicenseGrantedEvent>().As(_ =>
                    {
                        throw new InvalidOperationException("You can't do this at this moment.");
                    });

                    UseThe(new Transaction
                    {
                        Events = new[]
                        {
                            UseThe(new EventEnvelope
                            {
                                Body = The<LicenseGrantedEvent>()
                            })
                        }
                    });

                    The<MemoryEventSource>().Subscribe(0, Subject.Handle);
                });

                When(() => The<MemoryEventSource>().Write(The<Transaction>()), deferedExecution: true);
            }

            [Fact]
            public void Then_it_should_wrap_the_exception_into_a_projection_exception()
            {
                WhenAction.ShouldThrow<ProjectionException>()
                    .Where(e => e.CurrentEvent == The<EventEnvelope>())
                    .WithInnerException<InvalidOperationException>()
                    .WithInnerMessage("You can't do this at this moment.");
            }
        }
    }
}