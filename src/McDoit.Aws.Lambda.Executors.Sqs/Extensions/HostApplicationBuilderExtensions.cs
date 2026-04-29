using McDoit.Aws.Lambda.Executors.Sqs.Handlers;
using Microsoft.Extensions.Hosting;

namespace McDoit.Aws.Lambda.Executors.Sqs.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static SqsLambdaRegistrationBuilder<TMessage> AddSqsLambda<TMessage, THandler>(
        this IHostApplicationBuilder builder,
        Action<SqsLambdaRegistrationBuilder<TMessage>>? configure = null)
        where THandler : class, IMessageHandler<TMessage>
    {
        ArgumentNullException.ThrowIfNull(builder);
       return builder.Services.AddSqsLambda<TMessage, THandler>(configure);
	}
 public static SqsLambdaRegistrationBuilder<TMessage> AddSqsLambda<TMessage, THandler>(
		this IHostApplicationBuilder builder,
		Action<SqsLambdaRegistrationBuilder<TMessage>>? configure = null,
       bool rawAwareHandler = true)
		where THandler : class, ISqsMessageHandler<TMessage>
	{
		ArgumentNullException.ThrowIfNull(builder);

      if (!rawAwareHandler)
		{
			throw new ArgumentOutOfRangeException(
               nameof(rawAwareHandler),
				rawAwareHandler,
				"rawAwareHandler must be true.");
		}

       return builder.Services.AddSqsLambda<TMessage, THandler>(configure);
	}
}
