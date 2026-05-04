var builder = DistributedApplication.CreateBuilder(args);

builder.AddAWSLambdaFunction<Projects.CoreEventLambdaSample>("core-event", "CoreEventLambdaSample").WithExplicitStart();
builder.AddAWSLambdaFunction<Projects.CoreRequestResponseLambdaSample>("core-request-response", "CoreRequestResponseLambdaSample").WithExplicitStart();
builder.AddAWSLambdaFunction<Projects.SqsTypedProcessorSample>("sqs-typed", "SqsTypedProcessorSample").WithExplicitStart();
builder.AddAWSLambdaFunction<Projects.SqsRawAwareParallelSample>("sqs-raw-aware", "SqsRawAwareParallelSample").WithExplicitStart();
builder.AddAWSLambdaFunction<Projects.SnsTypedProcessorSample>("sns-typed", "SnsTypedProcessorSample").WithExplicitStart();
builder.AddAWSLambdaFunction<Projects.SnsRawAwareParallelSample>("sns-raw-aware", "SnsRawAwareParallelSample").WithExplicitStart();

builder.Build().Run();
