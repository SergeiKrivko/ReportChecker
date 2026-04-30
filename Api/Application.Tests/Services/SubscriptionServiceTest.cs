using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.Models;

namespace Application.Tests.Services
{
    [TestFixture]
    public class SubscriptionServiceTests
    {
        private ReportCheckerDbContext _context;
        private SubscriptionService _service;
        private IUserSubscriptionRepository _userSubscriptionRepository;
        private ISubscriptionPlanRepository _planRepository;
        private ISubscriptionOfferRepository _offerRepository;
        private ILlmUsageRepository _llmUsageRepository;
        private IReportRepository _reportRepository;
        private IPaymentRepository _paymentRepository;
        private IPaymentClient _paymentClient;
        private IConfiguration _configuration;

        [SetUp]
        public async Task SetUp()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ReportCheckerDbContext>()
                .UseSqlite(connection)
                .Options;

            _context = new ReportCheckerDbContext(options);

            await _context.Database.MigrateAsync("20260424151929_Subscriptions");

            // Initialize repositories with real DbContext
            _userSubscriptionRepository = new UserSubscriptionRepository(_context);
            _planRepository = new SubscriptionPlanRepository(_context);
            _offerRepository = new SubscriptionOfferRepository(_context);
            _llmUsageRepository = new LlmUsageRepository(_context);
            _reportRepository = new ReportRepository(_context);
            _paymentRepository = new PaymentRepository(_context);
            _paymentClient = new Mock<IPaymentClient>().Object;

            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["MaxFutureSubscriptions"]).Returns("3");
            _configuration = mockConfiguration.Object;

            _service = new SubscriptionService(
                _userSubscriptionRepository,
                _llmUsageRepository,
                _planRepository,
                _offerRepository,
                _reportRepository,
                _paymentRepository,
                _paymentClient,
                NullLogger<SubscriptionService>.Instance,
                _configuration);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        #region Test Data Setup Helpers

        private async Task<(SubscriptionPlan plan, SubscriptionOffer offer)> CreateTestPlanAndOfferAsync(
            string name = "Basic Plan",
            int tokensLimit = 100,
            int months = 1,
            decimal price = 100,
            bool isHidden = false)
        {
            var planId = await _planRepository.CreatePlanAsync(name, "Test Plan", tokensLimit,
                10, isHidden, CancellationToken.None);
            var plan = await _planRepository.GetPlanByIdAsync(planId);

            var offerId = await _offerRepository.CreateOfferAsync(planId, months, price, CancellationToken.None);
            var offer = await _offerRepository.GetOfferById(offerId);

            if (plan == null || offer == null)
                throw new Exception();
            return (plan, offer);
        }

        private async Task<UserSubscription> CreateActiveSubscriptionAsync(
            Guid userId,
            Guid planId,
            decimal defaultPricePerMonth,
            decimal price,
            DateTime startsAt,
            DateTime endsAt)
        {
            var subscription = await _userSubscriptionRepository.CreateSubscriptionAsync(
                planId, userId, defaultPricePerMonth, price, startsAt, endsAt, CancellationToken.None);

            await _userSubscriptionRepository.ConfirmSubscriptionAsync(subscription.Id, CancellationToken.None);
            return subscription;
        }

        #endregion

        #region Scenario 1: No Active Subscriptions

        [Test]
        public async Task CreateSubscriptionAsync_WithNoActiveSubscriptions_ShouldCreateNewActiveSubscription()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync("Premium Plan", 500, 3, 300);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            result.Subscription.PlanId.Should().Be(plan.Id);
            result.Subscription.UserId.Should().Be(userId);
            result.Subscription.Price.Should().Be(300);
            result.Subscription.ConfirmedAt.Should().BeNull();
            result.Subscription.StartsAt.Should().NotBeAfter(DateTime.UtcNow);
            result.Subscription.EndsAt.Should().Be(result.Subscription.StartsAt.AddDays(90));
            result.NextSubscriptions.Should().BeEmpty();
        }

        [Test]
        public async Task CreateSubscriptionAsync_WithOfferNotFound_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var invalidOfferId = Guid.NewGuid();

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, invalidOfferId, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
            result.Subscription.Should().BeNull();
        }

        [Test]
        public async Task CreateSubscriptionAsync_WithDeletedOffer_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync();

            await _offerRepository.DeleteOfferAsync(offer.Id, CancellationToken.None);

            // Act
            _context.ChangeTracker.Clear();
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
            result.Subscription.Should().BeNull();
        }

        #endregion

        #region Scenario 2: Same Plan (Up to 30 days remaining)

        [Test]
        public async Task CreateSubscriptionAsync_SamePlan_WithLessThan30DaysRemaining_ShouldCreateFutureSubscription()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription with 20 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(20);
            var activeSubscription = await CreateActiveSubscriptionAsync(
                userId, plan.Id, 100, 100, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            result.Subscription.StartsAt.Should().Be(endsAt);
            result.Subscription.EndsAt.Should().Be(endsAt.AddDays(30));
            result.NextSubscriptions.Should().BeEmpty();
        }

        [Test]
        public async Task CreateSubscriptionAsync_SamePlan_WithMoreThan30DaysRemaining_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription with 40 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-20);
            var endsAt = DateTime.UtcNow.AddDays(40);
            await CreateActiveSubscriptionAsync(userId, plan.Id, 100, 100, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task CreateSubscriptionAsync_SamePlan_WithFutureSubscriptionsExist_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription with 20 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(20);
            await CreateActiveSubscriptionAsync(userId, plan.Id, 100, 100, startsAt, endsAt);

            // Create future subscription
            var futureStartsAt = endsAt;
            var futureEndsAt = futureStartsAt.AddDays(30);
            await CreateActiveSubscriptionAsync(userId, plan.Id, 100, 100, futureStartsAt, futureEndsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Scenario 3: Upgrade (Better Plan)

        [Test]
        public async Task CreateSubscriptionAsync_Upgrade_WithLessThan30DaysRemaining_ShouldUpgradeImmediately()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 2, 250);

            // Create active subscription with 20 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(20);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 100, 100, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            result.Subscription.PlanId.Should().Be(newPlan.Id);
            result.Subscription.StartsAt.Should().NotBeAfter(DateTime.UtcNow);
            result.Subscription.Price.Should().BeLessThan(newOffer.Price); // Discount applied
            result.UnusedTokensDiscount.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task CreateSubscriptionAsync_Upgrade_WithMoreThan30DaysRemaining_ShouldUpgradeImmediately()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 2, 250);

            // Create active subscription with 50 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(50);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 100, 100, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            result.Subscription.PlanId.Should().Be(newPlan.Id);
            result.UnusedTokensDiscount.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task CreateSubscriptionAsync_Upgrade_WithFutureSubscriptions_ShouldUpgradeAndShiftRemaining()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (futurePlan, futureOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 1, 150);

            // Create active subscription
            var activeStartsAt = DateTime.UtcNow.AddDays(-10);
            var activeEndsAt = DateTime.UtcNow.AddDays(20);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 100, 100, activeStartsAt, activeEndsAt);

            // Create future subscription
            var futureStartsAt = activeEndsAt;
            var futureEndsAt = futureStartsAt.AddDays(30);
            await CreateActiveSubscriptionAsync(userId, futurePlan.Id, 100, 100, futureStartsAt, futureEndsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            result.Subscription.PlanId.Should().Be(newPlan.Id);
            // Should have remaining months shifted
            result.NextSubscriptions.Should().NotBeEmpty();
        }

        [Test]
        public async Task CreateSubscriptionAsync_Upgrade_WithBetterFuturePlan_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (betterFuturePlan, betterFutureOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 1, 200);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Standard", 150, 1, 150);

            // Create active subscription
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 100, 100,
                DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20));

            // Create better future subscription
            await CreateActiveSubscriptionAsync(userId, betterFuturePlan.Id, 200, 200,
                DateTime.UtcNow.AddDays(20), DateTime.UtcNow.AddDays(50));

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Scenario 4: Downgrade (Worse Plan)

        [Test]
        public async Task CreateSubscriptionAsync_Downgrade_WithLessThan30DaysRemaining_ShouldCreateFutureSubscription()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 1, 200);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription with 20 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(20);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 200, 200, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            result.Subscription.PlanId.Should().Be(newPlan.Id);
            result.Subscription.StartsAt.Should().Be(endsAt);
            result.Subscription.EndsAt.Should().Be(endsAt.AddDays(30));
        }

        [Test]
        public async Task CreateSubscriptionAsync_Downgrade_WithMoreThan30DaysRemaining_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 1, 200);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription with 40 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(40);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 200, 200, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task CreateSubscriptionAsync_Downgrade_WithFutureSubscriptions_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 1, 200);
            var (futurePlan, futureOffer) = await CreateTestPlanAndOfferAsync("Standard", 150, 1, 150);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 200, 200,
                DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20));

            // Create future subscription
            await CreateActiveSubscriptionAsync(userId, futurePlan.Id, 150, 150,
                DateTime.UtcNow.AddDays(20), DateTime.UtcNow.AddDays(50));

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Scenario 5: Unconfirmed Subscriptions Cleanup

        [Test]
        public async Task CreateSubscriptionAsync_WithUnconfirmedSubscriptions_ShouldDeleteThemAutomatically()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync();

            // Create unconfirmed subscription
            var unconfirmed = await _userSubscriptionRepository.CreateSubscriptionAsync(
                plan.Id, userId, 100, 100, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(60),
                CancellationToken.None);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            _context.ChangeTracker.Clear();
            result.ErrorText.Should().BeNull();

            // Verify unconfirmed subscription is deleted
            unconfirmed =
                await _userSubscriptionRepository.GetSubscriptionByIdAsync(unconfirmed.Id, CancellationToken.None);
            unconfirmed?.DeletedAt.Should().NotBeNull();
        }

        [Test]
        public async Task CreateSubscriptionAsync_WithConfirmedAndUnconfirmedSubscriptions_ShouldDeleteOnlyUnconfirmed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync();

            // Create confirmed active subscription
            await CreateActiveSubscriptionAsync(userId, plan.Id, 100, 100,
                DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20));

            // Create unconfirmed future subscription
            var unconfirmed = await _userSubscriptionRepository.CreateSubscriptionAsync(
                plan.Id, userId, 100, 100, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(60),
                CancellationToken.None);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            _context.ChangeTracker.Clear();
            result.ErrorText.Should().BeNull();

            unconfirmed =
                await _userSubscriptionRepository.GetSubscriptionByIdAsync(unconfirmed.Id, CancellationToken.None);
            unconfirmed?.DeletedAt.Should().NotBeNull();

            // Active subscription should still exist
            var active = await _userSubscriptionRepository.GetActiveSubscriptionAsync(userId, CancellationToken.None);
            active.Should().NotBeNull();
        }

        #endregion

        #region Scenario 6: Edge Cases

        [Test]
        public async Task CreateSubscriptionAsync_WithZeroMonthsOffer_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync(months: 0);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Scenario 7: Complex Upgrade with Overpayment

        [Test]
        public async Task CreateSubscriptionAsync_Upgrade_WithOverpayment_ShouldShiftRemainingMonths()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (futurePlan1, futureOffer1) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (futurePlan2, futureOffer2) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("Premium", 200, 1, 150);

            // Create active subscription with 60 days total (20 days left + 2 future months)
            var activeStartsAt = DateTime.UtcNow.AddDays(-40);
            var activeEndsAt = DateTime.UtcNow.AddDays(20);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, 100, 100, activeStartsAt, activeEndsAt);

            // Create two future subscriptions
            var future1StartsAt = activeEndsAt;
            var future1EndsAt = future1StartsAt.AddDays(30);
            await CreateActiveSubscriptionAsync(userId, futurePlan1.Id, 100, 100, future1StartsAt, future1EndsAt);

            var future2StartsAt = future1EndsAt;
            var future2EndsAt = future2StartsAt.AddDays(30);
            await CreateActiveSubscriptionAsync(userId, futurePlan2.Id, 100, 100, future2StartsAt, future2EndsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            result.ErrorText.Should().BeNull();
            result.Subscription.Should().NotBeNull();
            // Should have discount applied
            result.UnusedTokensDiscount.Should().BeGreaterThan(0);
            // Should have shifted remaining months
            result.NextSubscriptions.Should().NotBeEmpty();

            // Verify total length = 3 months original - 1 month new = 2 months shifted
            result.NextSubscriptions.Count.Should().Be(2);
        }

        #endregion

        #region Scenario 8: Multiple Test Cases

        [TestCase(15, true)]
        [TestCase(25, true)]
        [TestCase(29, true)]
        [TestCase(30, true)]
        [TestCase(31, false)]
        [TestCase(45, false)]
        public async Task CreateSubscriptionAsync_SamePlan_WithVariousRemainingDays_ShouldRespect30DayRule(
            int remainingDays, bool shouldSucceed)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (plan, offer) = await CreateTestPlanAndOfferAsync("Basic", 100, 1, 100);

            // Create active subscription with specified remaining days
            var startsAt = DateTime.UtcNow.AddDays(-(30 - remainingDays));
            var endsAt = DateTime.UtcNow.AddDays(remainingDays);
            await CreateActiveSubscriptionAsync(userId, plan.Id, 100, 100, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, offer.Id, CancellationToken.None);

            // Assert
            if (shouldSucceed)
            {
                result.ErrorText.Should().BeNull();
                result.Subscription.Should().NotBeNull();
            }
            else
            {
                result.ErrorText.Should().NotBeNull();
                result.Subscription.Should().BeNull();
            }
        }

        [TestCase(100, 200, true)] // Upgrade
        [TestCase(200, 100, true)] // Downgrade with <30 days
        [TestCase(100, 100, true)] // Same plan with <30 days
        public async Task CreateSubscriptionAsync_WithLessThan30DaysRemaining_ShouldAllowAllOperations(
            int oldTokensLimit, int newTokensLimit, bool shouldSucceed)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var (oldPlan, oldOffer) = await CreateTestPlanAndOfferAsync("Old Plan", oldTokensLimit, 1, oldTokensLimit);
            var (newPlan, newOffer) = await CreateTestPlanAndOfferAsync("New Plan", newTokensLimit, 1, newTokensLimit);

            // Create active subscription with 20 days remaining
            var startsAt = DateTime.UtcNow.AddDays(-10);
            var endsAt = DateTime.UtcNow.AddDays(20);
            await CreateActiveSubscriptionAsync(userId, oldPlan.Id, oldTokensLimit, oldTokensLimit, startsAt, endsAt);

            // Act
            var result = await _service.CreateSubscriptionAsync(userId, newOffer.Id, CancellationToken.None);

            // Assert
            if (shouldSucceed)
            {
                result.ErrorText.Should().BeNull();
                result.Subscription.Should().NotBeNull();
            }
            else
            {
                result.ErrorText.Should().NotBeNull();
                result.Subscription.Should().BeNull();
            }
        }

        #endregion
    }
}