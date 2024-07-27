using Microsoft.Extensions.Options;
using Reserve.API.Infrastructure;
using Reserve.API.Services;

namespace Reserve.API.Model;

public class ReserveServices(
    ReserveContext context,
    IOptions<ReserveOptions> options,
    ILogger<ReserveServices> logger,
    EncryptionService encryptionService,
    EmailService emailService
)
{
    public ReserveContext Context { get; } = context;
    public IOptions<ReserveOptions> Options { get; } = options;
    public ILogger<ReserveServices> Logger { get; } = logger;
    public EncryptionService EncryptionService { get; } = encryptionService;
    public EmailService EmailService { get; } = emailService;
};