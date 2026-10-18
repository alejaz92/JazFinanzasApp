using System.Text.Json;
using FluentAssertions;
using JazFinanzasApp.API.Business.Assistant;
using JazFinanzasApp.API.Business.DTO.Assistant;
using JazFinanzasApp.API.Business.Exceptions;
using JazFinanzasApp.API.Domain;
using Microsoft.EntityFrameworkCore;
using static JazFinanzasApp.Tests.Services.AssistantReviewServiceTests;

namespace JazFinanzasApp.Tests.Services
{
    // plan-devoluciones-llm.md, Fase 8 — hallazgos del asistente (D8b y D15) y revisión semanal (D6, paso 3), con un
    // cliente del modelo falso. Ningún test llama a la API real. Reusa el armado de AssistantReviewServiceTests.
    public class AssistantFindingsAndWeeklyScanTests
    {
        private record F(string Title, string Detail, string? Category, string Kind);

        private static readonly F Delivery = new("Delivery se duplicó", "Delivery llegó a $85.000.", "Delivery", "unusual-amount");
        private static readonly F Movilidad = new("Movilidad sube", "Es el tercer mes seguido que sube.", "Movilidad", "trend");
        private static readonly F SinSuper = new("Casi sin supermercado", "¿Falta cargar algo? Hay $85.000 menos.", "Supermercado", "missing-data");
        private static readonly F Otro = new("Algo raro", "Detalle sin categoría.", null, "other");

        private static string Review(params F[] findings) => JsonSerializer.Serialize(new
        {
            headline = "El mes cerró con un gasto de $85.000",
            highlights = new[] { "Primero", "Segundo", "Tercero" },
            body = "El ahorro fue de 18 %.",
            findings = findings.Select(f => new { title = f.Title, detail = f.Detail, category = f.Category, kind = f.Kind })
        });

        private static string Weekly(params F[] findings) => JsonSerializer.Serialize(new
        {
            findings = findings.Select(f => new { title = f.Title, detail = f.Detail, category = f.Category, kind = f.Kind })
        });

        private static async Task<Fixture> WithReviewAsync(params F[] findings)
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(findings));
            await f.Service.GenerateAsync(UserId, Close());
            return f;
        }

        private static DateTime Utc(int month, int day, int hour = 12) => new(2026, month, day, hour, 0, 0, DateTimeKind.Utc);

        // La semana del 12 al 18 de octubre de 2026 (el 17 es sábado). Un cierre generado el jueves 8 queda en la
        // semana anterior; el miércoles 14 es una semana nueva en la que todavía no hace falta la mitad de mes.
        private static async Task<Fixture> DueFixtureAsync()
        {
            var f = new Fixture(time: new MutableTimeProvider(Utc(10, 8)));
            f.AddActivityInSeptember();
            await f.Service.GenerateAsync(UserId, Close());
            f.Time.Now = Utc(10, 14);
            f.AddActivity(new DateTime(2026, 10, 13), loadedAt: Utc(10, 13, 9)); // cargado después de la última pasada
            return f;
        }

        private static GenerateAssistantReviewDTO Scan(bool regenerate = false) =>
            new() { Kind = AssistantReviewKind.WeeklyScan, Regenerate = regenerate };

        // --- hallazgos de una devolución ---

        [Fact]
        public async Task TheFindingsOfAReviewAreSavedAsActiveWithTheirReviewAndCategory()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Context.TransactionClasses.Add(new TransactionClass { Description = "Delivery", IncExp = "E", CountsAsIncomeExpense = true, UserId = UserId });
            f.Context.SaveChanges();
            f.Client.Respond = () => Ok(Review(Delivery, Otro));

            var review = await f.Service.GenerateAsync(UserId, Close());

            var findings = await f.Findings.GetAsync(UserId, includeAll: false);
            findings.Should().HaveCount(2);
            findings.Should().OnlyContain(x => x.ReviewId == review.Id && x.Status == AssistantFindingStatus.Active && x.Rating == null);
            var delivery = findings.Single(x => x.Title == Delivery.Title);
            delivery.Detail.Should().Be(Delivery.Detail);
            delivery.TransactionClassId.Should().Be(f.Context.TransactionClasses.Single().Id);
            findings.Single(x => x.Title == Otro.Title).TransactionClassId.Should().BeNull();
        }

        [Fact]
        public async Task ACategoryThatDoesNotExistOrIsAmbiguousHasNoLink()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Context.TransactionClasses.AddRange(
                new TransactionClass { Description = "Ocio", IncExp = "E", CountsAsIncomeExpense = true, UserId = UserId },
                new TransactionClass { Description = "ocio", IncExp = "E", CountsAsIncomeExpense = true, UserId = UserId });
            f.Context.SaveChanges();
            f.Client.Respond = () => Ok(Review(new F("Ocio sube", "Detalle.", "Ocio", "trend"), SinSuper));

            await f.Service.GenerateAsync(UserId, Close());

            (await f.Findings.GetAsync(UserId, false)).Should().OnlyContain(x => x.TransactionClassId == null);
        }

        [Fact]
        public async Task AReviewWithoutFindingsKeyOrWithAnEmptyListSavesNone()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            await f.Service.GenerateAsync(UserId, Close()); // GoodOutput de la Fase 7: sin la clave findings

            (await f.Findings.GetAsync(UserId, true)).Should().BeEmpty();

            f.Client.Respond = () => Ok(Review());
            var regenerated = await f.Service.GenerateAsync(UserId, Close(regenerate: true));
            regenerated.Status.Should().Be(AssistantReviewStatus.Ready);
            (await f.Findings.GetAsync(UserId, true)).Should().BeEmpty();
        }

        [Fact]
        public async Task AtMostThreeNewFindingsAreSavedPerReview()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(Delivery, Movilidad, SinSuper, Otro, new F("Quinto", "Detalle.", "Ocio", "trend")));

            await f.Service.GenerateAsync(UserId, Close());

            (await f.Findings.GetAsync(UserId, false)).Select(x => x.Title).Should().BeEquivalentTo(Delivery.Title, Movilidad.Title, SinSuper.Title);
        }

        [Fact]
        public async Task FindingsWithoutTitleOrDetailAreIgnoredAndDoNotTakeASlot()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(new F("  ", "Detalle.", "Ocio", "trend"), new F("Título", "", "Ocio", "trend"), Delivery));

            await f.Service.GenerateAsync(UserId, Close());

            (await f.Findings.GetAsync(UserId, true)).Should().ContainSingle().Which.Title.Should().Be(Delivery.Title);
        }

        [Fact]
        public async Task ATooLongTitleAndDetailAreTrimmedToTheirColumns()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(new F(new string('t', 500), new string('d', 2000), "Ocio", "trend")));

            await f.Service.GenerateAsync(UserId, Close());

            var saved = (await f.Findings.GetAsync(UserId, false)).Single();
            saved.Title.Length.Should().Be(200);
            saved.Detail.Length.Should().Be(1000);
        }

        // --- no repetir (D15) ---

        [Fact]
        public async Task AFindingWithAFingerprintAlreadySeenThisMonthIsNotSavedAgainEvenIfItIsActive()
        {
            var f = await WithReviewAsync(Delivery);

            await f.Service.GenerateAsync(UserId, Close(regenerate: true));

            (await f.Findings.GetAsync(UserId, true)).Should().ContainSingle("el repetido no se muestra");
            var rows = f.Context.AssistantFindings.OrderBy(x => x.Id).ToList();
            rows.Select(x => x.Status).Should().Equal(AssistantFindingStatus.Active, AssistantFindingStatus.Repeated);
            rows[1].Title.Should().Be(Delivery.Title, "queda guardado para que la revisión de calidad vea cuánto insiste");
        }

        [Fact]
        public async Task ADismissedFindingDoesNotComeBackInTheSameMonth()
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;
            await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Dismiss = true });

            await f.Service.GenerateAsync(UserId, Close(regenerate: true));

            (await f.Findings.GetAsync(UserId, false)).Should().BeEmpty();
            (await f.Findings.GetAsync(UserId, true)).Should().ContainSingle().Which.Status.Should().Be(AssistantFindingStatus.Dismissed);
        }

        [Fact]
        public async Task TheSameFindingAFewMonthsAgoDoesNotBlockANewOne()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Context.AssistantFindings.Add(new AssistantFinding
            {
                ReviewId = 99, UserId = UserId, Title = "Viejo", Detail = "d", Fingerprint = "delivery|unusual-amount",
                Status = AssistantFindingStatus.Dismissed, ExpiresAt = Utc(9, 30), CreatedAt = Utc(9, 20)
            });
            f.Context.SaveChanges();
            f.Client.Respond = () => Ok(Review(Delivery));

            await f.Service.GenerateAsync(UserId, Close());

            (await f.Findings.GetAsync(UserId, false)).Should().ContainSingle().Which.Title.Should().Be(Delivery.Title);
        }

        [Fact]
        public async Task TwoFindingsWithTheSameCategoryAndKindInOneResponseSaveOnlyTheFirst()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(Delivery, new F("Otra vez Delivery", "Otro detalle.", "delivery", "unusual-amount")));

            await f.Service.GenerateAsync(UserId, Close());

            (await f.Findings.GetAsync(UserId, false)).Should().ContainSingle().Which.Title.Should().Be(Delivery.Title);
            f.Context.AssistantFindings.Count(x => x.Status == AssistantFindingStatus.Repeated).Should().Be(1);
        }

        [Fact]
        public async Task ARepeatedFindingDoesNotTakeASlotFromTheNewOnes()
        {
            var f = await WithReviewAsync(Delivery);
            f.Client.Respond = () => Ok(Review(Delivery, Movilidad, SinSuper, Otro));

            await f.Service.GenerateAsync(UserId, Close(regenerate: true));

            (await f.Findings.GetAsync(UserId, false)).Select(x => x.Title)
                .Should().BeEquivalentTo(Delivery.Title, Movilidad.Title, SinSuper.Title, Otro.Title);
        }

        [Theory]
        [InlineData("Delivery", "unusual-amount", "delivery|unusual-amount")]
        [InlineData("  DELIVERY  ", "Unusual-Amount", "delivery|unusual-amount")]
        [InlineData("Impuestos   y Servicios", "trend", "impuestos y servicios|trend")]
        [InlineData("Bares, salidas, juntadas", "trend", "bares, salidas, juntadas|trend")]
        [InlineData("Ócio", "trend", "ocio|trend")]
        [InlineData(null, "other", "general|other")]
        [InlineData("", null, "general|other")]
        public void TheFingerprintIsTheCategoryPlusTheKindNormalized(string? category, string? kind, string expected)
        {
            AssistantFindingService.BuildFingerprint(category, kind).Should().Be(expected);
        }

        [Fact]
        public void DifferentKindsOrCategoriesGiveDifferentFingerprints()
        {
            AssistantFindingService.BuildFingerprint("Delivery", "trend")
                .Should().NotBe(AssistantFindingService.BuildFingerprint("Delivery", "unusual-amount"));
            AssistantFindingService.BuildFingerprint("Delivery", "trend")
                .Should().NotBe(AssistantFindingService.BuildFingerprint("Movilidad", "trend"));
        }

        // --- vencimiento ---

        [Fact]
        public async Task AFindingExpiresInFourteenDaysWhenThatComesBeforeTheEndOfTheMonth()
        {
            var f = await WithReviewAsync(Delivery); // 17 de octubre: +14 días es el 31 a las 12, antes del fin de mes

            (await f.Findings.GetAsync(UserId, false)).Single().ExpiresAt.Should().Be(Utc(10, 31));
        }

        [Fact]
        public async Task AFindingExpiresAtTheEndOfTheMonthWhenThatComesFirst()
        {
            var f = new Fixture(time: new MutableTimeProvider(Utc(10, 25)));
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(Delivery));

            await f.Service.GenerateAsync(UserId, Close()); // 25 de octubre: +14 días sería el 8 de noviembre

            (await f.Findings.GetAsync(UserId, false)).Single().ExpiresAt.Should().Be(new DateTime(2026, 11, 1));
        }

        [Fact]
        public async Task AnExpiredFindingBecomesExpiredWhenItIsRead()
        {
            var f = await WithReviewAsync(Delivery, Movilidad);
            f.Time.Now = Utc(10, 31, 13); // pasó el vencimiento de los dos

            (await f.Findings.GetAsync(UserId, false)).Should().BeEmpty();

            f.Context.AssistantFindings.Should().OnlyContain(x => x.Status == AssistantFindingStatus.Expired);
            (await f.Findings.GetAsync(UserId, true)).Should().HaveCount(2).And.OnlyContain(x => x.Status == AssistantFindingStatus.Expired);
        }

        [Fact]
        public async Task OnlyAnActiveFindingExpires()
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;
            await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Dismiss = true });
            f.Time.Now = Utc(11, 20);

            (await f.Findings.GetAsync(UserId, true)).Single().Status.Should().Be(AssistantFindingStatus.Dismissed);
        }

        // --- control de cifras ---

        [Fact]
        public async Task AFigureThatIsNotInTheContextMarksTheFindingButDoesNotBlockIt()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => Ok(Review(new F("Delivery", "Gastó $999.999 en el mes.", "Delivery", "unusual-amount"), Movilidad));

            await f.Service.GenerateAsync(UserId, Close());

            var findings = await f.Findings.GetAsync(UserId, false);
            findings.Single(x => x.Title == "Delivery").UnverifiedFigures.Should().Equal("$999.999");
            findings.Single(x => x.Title == Movilidad.Title).UnverifiedFigures.Should().BeEmpty();
        }

        [Fact]
        public async Task AFigureThatIsInTheContextDoesNotMarkTheFinding()
        {
            var f = await WithReviewAsync(Delivery); // "$85.000" está en el contexto (85.432)

            (await f.Findings.GetAsync(UserId, false)).Single().UnverifiedFigures.Should().BeEmpty();
        }

        // --- cuándo no se guardan ---

        [Fact]
        public async Task AFailedReviewSavesNoFindings()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();
            f.Client.Respond = () => new AssistantModelResponse("claude-opus-5-5", Review(Delivery), "max_tokens", new AssistantModelUsage(9_000, 16_000, 0, 0));

            var review = await f.Service.GenerateAsync(UserId, Close());

            review.Status.Should().Be(AssistantReviewStatus.Failed);
            f.Context.AssistantFindings.Should().BeEmpty();
        }

        [Fact]
        public async Task APreviewDoesNotSaveFindings()
        {
            var f = await WithReviewAsync();
            var original = f.Context.AssistantReviews.Single();
            f.Client.Respond = () => Ok(Review(Movilidad));

            await f.Service.PreviewAsync(UserId, original.Id, "v1");

            f.Context.AssistantFindings.Should().BeEmpty();
        }

        // --- GET findings y PUT findings/{id} ---

        [Fact]
        public async Task DismissingMovesTheFindingToDismissedAndItLeavesTheActiveList()
        {
            var f = await WithReviewAsync(Delivery, Movilidad);
            var first = (await f.Findings.GetAsync(UserId, false)).First();

            var updated = await f.Findings.UpdateAsync(UserId, first.Id, new UpdateAssistantFindingDTO { Dismiss = true });

            updated.Status.Should().Be(AssistantFindingStatus.Dismissed);
            (await f.Findings.GetAsync(UserId, false)).Should().ContainSingle().Which.Id.Should().NotBe(first.Id);
            (await f.Findings.GetAsync(UserId, true)).Should().HaveCount(2);
        }

        [Fact]
        public async Task RatingStoresTheRatingAndCanBeChanged()
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;

            (await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Rating = 1 })).Rating.Should().Be(1);
            var second = await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Rating = -1 });

            second.Rating.Should().Be(-1);
            second.Status.Should().Be(AssistantFindingStatus.Active, "calificar no lo descarta");
            f.Context.AssistantFindings.Single().Rating.Should().Be(-1);
        }

        [Fact]
        public async Task ADismissedFindingCanStillBeRatedAndItKeepsBeingDismissed()
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;

            var updated = await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Dismiss = true, Rating = -1 });

            updated.Status.Should().Be(AssistantFindingStatus.Dismissed);
            updated.Rating.Should().Be(-1);
            (await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Dismiss = true })).Status.Should().Be(AssistantFindingStatus.Dismissed);
        }

        [Fact]
        public async Task AnExpiredFindingCannotBeDismissedButCanBeRated()
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;
            f.Time.Now = Utc(11, 20);

            var dismiss = () => f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Dismiss = true });
            await dismiss.Should().ThrowAsync<BusinessRuleException>();

            (await f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Rating = 1 })).Rating.Should().Be(1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task ARatingThatIsNotUsefulOrNotUsefulIsRejected(int rating)
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;

            var act = () => f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Rating = rating });

            await act.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AnUpdateWithNeitherDismissNorRatingIsRejected()
        {
            var f = await WithReviewAsync(Delivery);
            var id = (await f.Findings.GetAsync(UserId, false)).Single().Id;

            var act = () => f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO());

            await act.Should().ThrowAsync<BusinessRuleException>();
            var notDismissing = () => f.Findings.UpdateAsync(UserId, id, new UpdateAssistantFindingDTO { Dismiss = false });
            await notDismissing.Should().ThrowAsync<BusinessRuleException>();
        }

        [Fact]
        public async Task AFindingOfAnotherUserOrARepeatedOneIsNotFound()
        {
            var f = await WithReviewAsync(Delivery);
            await f.Service.GenerateAsync(UserId, Close(regenerate: true)); // deja un Repeated
            var repeatedId = f.Context.AssistantFindings.Single(x => x.Status == AssistantFindingStatus.Repeated).Id;
            var activeId = f.Context.AssistantFindings.Single(x => x.Status == AssistantFindingStatus.Active).Id;
            f.Context.AssistantUserSettings.Add(new AssistantUserSettings { UserId = 2, Enabled = true, MonthlyBudgetUsd = 3m });
            f.Context.SaveChanges();

            await FluentActions.Awaiting(() => f.Findings.UpdateAsync(UserId, repeatedId, new UpdateAssistantFindingDTO { Dismiss = true })).Should().ThrowAsync<NotFoundException>();
            await FluentActions.Awaiting(() => f.Findings.UpdateAsync(2, activeId, new UpdateAssistantFindingDTO { Dismiss = true })).Should().ThrowAsync<NotFoundException>();
            (await f.Findings.GetAsync(2, true)).Should().BeEmpty();
        }

        [Fact]
        public async Task TheFindingsEndpointsRejectAUserWithoutTheAssistant()
        {
            var f = new Fixture(enabled: false);

            await FluentActions.Awaiting(() => f.Findings.GetAsync(UserId, false)).Should().ThrowAsync<BusinessRuleException>().WithMessage("El asistente no está habilitado para tu usuario");
            await FluentActions.Awaiting(() => f.Findings.UpdateAsync(UserId, 1, new UpdateAssistantFindingDTO { Dismiss = true })).Should().ThrowAsync<BusinessRuleException>();
        }

        // --- WeeklyScanDue (D6, paso 3) ---

        [Fact]
        public async Task TheWeeklyScanIsDueWhenAllThreeConditionsHold()
        {
            var f = await DueFixtureAsync();

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.Ready);
            latest.WeeklyScanDue.Should().BeTrue();
        }

        [Fact]
        public async Task ItIsNotDueWhenThereWasAlreadyAPassThisWeek()
        {
            var f = await DueFixtureAsync();
            await f.Service.GenerateAsync(UserId, Close(regenerate: true)); // una devolución el miércoles cuenta como pasada

            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeFalse();
        }

        [Fact]
        public async Task ItIsNotDueWhenNoMovementWasLoadedSinceTheLastPass()
        {
            var f = new Fixture(time: new MutableTimeProvider(Utc(10, 8)));
            f.AddActivityInSeptember();                       // cargado a la misma hora que la última pasada
            await f.Service.GenerateAsync(UserId, Close());
            f.Time.Now = Utc(10, 14);

            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeFalse();

            f.AddActivity(new DateTime(2026, 10, 13), loadedAt: Utc(10, 13, 9));
            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeTrue();
        }

        [Fact]
        public async Task ItIsNotDueOncePastEightyPercentOfTheBudget()
        {
            var f = await DueFixtureAsync();
            f.Context.AssistantApiCalls.Add(new AssistantApiCall { UserId = UserId, Purpose = AssistantApiCallPurpose.Review, Model = "claude-opus-5-5", CostUsd = 2.4m, Succeeded = true, CreatedAt = Utc(10, 10) });
            f.Context.SaveChanges();

            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeFalse();
        }

        [Fact]
        public async Task ItIsNotDueWhenADevolutionIsMissingBecauseTheDevolutionComesFirst()
        {
            var f = new Fixture();
            f.AddActivityInSeptember();

            var latest = await f.Service.GetLatestAsync(UserId);

            latest.State.Should().Be(AssistantLatestState.NotGenerated);
            latest.WeeklyScanDue.Should().BeFalse();
        }

        [Fact]
        public async Task AFailedWeeklyScanAlsoCountsAsAPassSoItDoesNotRetryOnEveryVisit()
        {
            var f = await DueFixtureAsync();
            f.Client.Throw = new AssistantModelException();
            (await f.Service.GenerateAsync(UserId, Scan())).Status.Should().Be(AssistantReviewStatus.Failed);

            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeFalse();

            f.Time.Now = f.Time.Now.AddHours(2);
            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeTrue("pasada la hora de espera se puede reintentar");
        }

        // --- generar la revisión semanal ---

        [Fact]
        public async Task TheWeeklyScanSavesOnlyFindingsWithItsOwnInstructionsAndMediumEffort()
        {
            var f = await DueFixtureAsync();
            f.Client.Respond = () => Ok(Weekly(Delivery, Movilidad));

            var dto = await f.Service.GenerateAsync(UserId, Scan());

            dto.Status.Should().Be(AssistantReviewStatus.Ready);
            dto.Kind.Should().Be(AssistantReviewKind.WeeklyScan);
            dto.PeriodStart.Should().Be(new DateTime(2026, 10, 12), "el lunes de la semana");
            dto.CutoffDate.Should().Be(new DateTime(2026, 10, 13), "los datos llegan hasta ayer");
            dto.PromptVersion.Should().Be("weekly-scan.v1");
            dto.Headline.Should().BeNull();
            dto.Body.Should().BeNull();
            dto.Highlights.Should().BeEmpty();

            var request = f.Client.Requests.Last();
            request.Effort.Should().Be(AssistantEffort.Medium);
            request.SystemPrompt.Should().Contain("REVISIÓN SEMANAL");
            request.OutputSchemaJson.Should().Contain("findings").And.NotContain("headline");

            var call = f.Context.AssistantApiCalls.OrderBy(c => c.Id).Last();
            call.Purpose.Should().Be(AssistantApiCallPurpose.WeeklyScan);
            call.ReviewId.Should().Be(dto.Id);

            (await f.Findings.GetAsync(UserId, false)).Select(x => x.Title).Should().BeEquivalentTo(Delivery.Title, Movilidad.Title);
            f.Context.AssistantFindings.Should().OnlyContain(x => x.ReviewId == dto.Id);
        }

        [Fact]
        public async Task AWeeklyScanWithNoFindingsIsAValidAnswer()
        {
            var f = await DueFixtureAsync();
            f.Client.Respond = () => Ok(Weekly());

            var dto = await f.Service.GenerateAsync(UserId, Scan());

            dto.Status.Should().Be(AssistantReviewStatus.Ready);
            f.Context.AssistantFindings.Should().BeEmpty();
            (await f.Service.GetLatestAsync(UserId)).WeeklyScanDue.Should().BeFalse("la pasada ya se hizo esta semana");
        }

        [Fact]
        public async Task AWeeklyScanWithoutTheFindingsFieldIsMalformed()
        {
            var f = await DueFixtureAsync();
            f.Client.Respond = () => Ok("{}");

            var dto = await f.Service.GenerateAsync(UserId, Scan());

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Contain("formato esperado");
        }

        [Fact]
        public async Task AWeeklyScanDoesNotAppearInTheHistoryOfReviews()
        {
            var f = await DueFixtureAsync();
            f.Client.Respond = () => Ok(Weekly(Delivery));
            await f.Service.GenerateAsync(UserId, Scan());

            (await f.Service.GetHistoryAsync(UserId, null)).Should().OnlyContain(r => r.Kind != AssistantReviewKind.WeeklyScan);
        }

        [Fact]
        public async Task AWeeklyScanIsRejectedWhenItIsNotDue()
        {
            var f = await DueFixtureAsync();
            await f.Service.GenerateAsync(UserId, Close(regenerate: true)); // ya hubo una pasada esta semana
            var callsBefore = f.Client.Requests.Count;

            var act = () => f.Service.GenerateAsync(UserId, Scan());

            await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*No hace falta una revisión semanal*");
            f.Client.Requests.Count.Should().Be(callsBefore);
        }

        [Fact]
        public async Task AWeeklyScanCanBeForcedByRegenerating()
        {
            var f = await DueFixtureAsync();
            await f.Service.GenerateAsync(UserId, Close(regenerate: true));
            f.Client.Respond = () => Ok(Weekly(Delivery));

            var dto = await f.Service.GenerateAsync(UserId, Scan(regenerate: true));

            dto.Status.Should().Be(AssistantReviewStatus.Ready);
        }

        [Fact]
        public async Task AskingForTheWeeklyScanTwiceReturnsTheSameOneWithoutCallingTheModelAgain()
        {
            var f = await DueFixtureAsync();
            f.Client.Respond = () => Ok(Weekly(Delivery));
            var first = await f.Service.GenerateAsync(UserId, Scan());
            var calls = f.Client.Requests.Count;

            var second = await f.Service.GenerateAsync(UserId, Scan());

            second.Id.Should().Be(first.Id);
            f.Client.Requests.Count.Should().Be(calls);
        }

        [Fact]
        public async Task AWeeklyScanPastEightyPercentOfTheBudgetIsNotRunAndLeavesNoFailedRow()
        {
            var f = await DueFixtureAsync();
            f.Context.AssistantApiCalls.Add(new AssistantApiCall { UserId = UserId, Purpose = AssistantApiCallPurpose.Review, Model = "claude-opus-5-5", CostUsd = 2.5m, Succeeded = true, CreatedAt = Utc(10, 10) });
            f.Context.SaveChanges();
            var callsBefore = f.Client.Requests.Count;

            var act = () => f.Service.GenerateAsync(UserId, Scan());

            await act.Should().ThrowAsync<BusinessRuleException>();
            f.Client.Requests.Count.Should().Be(callsBefore);
            f.Context.AssistantReviews.Any(r => r.Kind == AssistantReviewKind.WeeklyScan).Should().BeFalse();
        }

        [Fact]
        public async Task AWeeklyScanOnAForcedRegenerationPastTheLimitFailsWithTheBudgetMessage()
        {
            var f = await DueFixtureAsync();
            f.Context.AssistantApiCalls.Add(new AssistantApiCall { UserId = UserId, Purpose = AssistantApiCallPurpose.Review, Model = "claude-opus-5-5", CostUsd = 2.5m, Succeeded = true, CreatedAt = Utc(10, 10) });
            f.Context.SaveChanges();

            var dto = await f.Service.GenerateAsync(UserId, Scan(regenerate: true));

            dto.Status.Should().Be(AssistantReviewStatus.Failed);
            dto.ErrorMessage.Should().Contain("revisión semanal");
        }

        [Fact]
        public async Task TheRepeatedFindingFromAWeeklyScanIsDiscardedAgainstTheDevolutionOnes()
        {
            var f = await DueFixtureAsync();
            f.Context.AssistantFindings.Add(new AssistantFinding
            {
                ReviewId = 1, UserId = UserId, Title = "Ya avisado", Detail = "d", Fingerprint = "delivery|unusual-amount",
                Status = AssistantFindingStatus.Active, ExpiresAt = Utc(10, 31), CreatedAt = Utc(10, 9)
            });
            f.Context.SaveChanges();
            f.Client.Respond = () => Ok(Weekly(Delivery, Movilidad));

            await f.Service.GenerateAsync(UserId, Scan());

            (await f.Findings.GetAsync(UserId, false)).Select(x => x.Title).Should().BeEquivalentTo("Ya avisado", Movilidad.Title);
        }

        // --- instrucciones y esquemas ---

        [Fact]
        public void EveryCurrentPromptAsksForFindingsAndTheWeeklyOneOnlyForThem()
        {
            AssistantPrompts.Load(AssistantPrompts.MonthCloseCurrent).Should().Contain("# Hallazgos").And.Contain("`findings`");
            AssistantPrompts.Load(AssistantPrompts.MidMonthCurrent).Should().Contain("# Hallazgos").And.Contain("`findings`");
            var weekly = AssistantPrompts.Load(AssistantPrompts.WeeklyScanCurrent);
            weekly.Should().Contain("REVISIÓN SEMANAL").And.Contain("Nunca calcules").And.Contain("lista vacía");
            AssistantPrompts.CurrentVersion(AssistantReviewKind.WeeklyScan).Should().Be("weekly-scan.v1");
        }

        [Fact]
        public void TheSchemasListTheFourFindingKindsAndRequireTheirFields()
        {
            foreach (var schema in new[] { AssistantPrompts.ReviewOutputSchema, AssistantPrompts.WeeklyScanOutputSchema })
            {
                using var doc = JsonDocument.Parse(schema);
                var item = doc.RootElement.GetProperty("properties").GetProperty("findings").GetProperty("items");
                item.GetProperty("required").EnumerateArray().Select(e => e.GetString())
                    .Should().BeEquivalentTo("title", "detail", "category", "kind");
                item.GetProperty("properties").GetProperty("kind").GetProperty("enum").EnumerateArray().Select(e => e.GetString())
                    .Should().BeEquivalentTo(AssistantPrompts.FindingKinds);
            }

            using var weekly = JsonDocument.Parse(AssistantPrompts.WeeklyScanOutputSchema);
            weekly.RootElement.GetProperty("required").EnumerateArray().Select(e => e.GetString()).Should().Equal("findings");
            AssistantPrompts.OutputSchema(AssistantReviewKind.WeeklyScan).Should().Be(AssistantPrompts.WeeklyScanOutputSchema);
            AssistantPrompts.OutputSchema(AssistantReviewKind.MonthClose).Should().Be(AssistantPrompts.ReviewOutputSchema);
        }
    }
}
