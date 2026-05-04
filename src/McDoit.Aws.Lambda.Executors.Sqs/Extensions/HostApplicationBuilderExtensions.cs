using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sqs.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static SqsLambdaRegistrationBuilder<TMessage> AddSqsLambda<TMessage, TProcessor>(
		this IHostApplicationBuilder builder,
		Action<SqsLambdaRegistrationBuilder<TMessage>>? configure = null)
      where TProcessor : class, ISqsMessageProcessor<TMessage>
	{
		ArgumentNullException.ThrowIfNull(builder);
       return builder.Services.AddSqsLambda<TMessage, TProcessor>(configure);
	}
}
