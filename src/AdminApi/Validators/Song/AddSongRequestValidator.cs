#region Usings

using FluentValidation;
using UltimatePlaylist.AdminApi.Models.Song;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Mvc.Extensions;

#endregion

namespace UltimatePlaylist.AdminApi.Validators.Song
{
    public class AddSongRequestValidator : AbstractValidator<AddSongRequestModel>
    {
        public AddSongRequestValidator()
        {
            RuleFor(p => p.SongFileExternalId)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.SongCoverExternalId)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.Artist)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.Title)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.Album)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.Label)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.Licensor)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.LinkToSpotify)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty)
                .MustBeSpotifyDeepLink();

            //RuleFor(p => p.LinkToAppleMusic)
            //    .NotEmpty()
            //    .WithMessage(ValidationMessages.CannotBeEmpty)
            //    .MustBeAppleMusicDeepLink();

            RuleFor(p => p.InstagramUrl)
                .MustBeInstagramLink();

            RuleFor(p => p.FacebookUrl)
                .MustBeFacebookLink();

            RuleFor(p => p.YoutubeUrl)
                .MustBeYoutubeLink();

            RuleFor(p => p.SnapchatUrl)
                .MustBeSnapchatLink();

            RuleFor(p => p.IsNewRelease)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.FirstPublicReleaseDate)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.PrimaryGenres)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.IsAllAudioOriginal)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.IsAllArtworkOriginal)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.IsSongWithExplicitContent)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.IsSongWithSample)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.SongWriter)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.Producer)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            RuleFor(p => p.IsAllConfirmed)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty);

            When(x => x.IsSongWithSample == true, () =>
            {
                RuleFor(p => p.IsLeagalClearanceObtained)
                    .NotEmpty()
                    .WithMessage(ValidationMessages.CannotBeEmpty);
            });

            RuleFor(p => p.Duration)
                .NotEmpty()
                .WithMessage(ValidationMessages.CannotBeEmpty)
                .SongDurationMinumum();
        }
    }
}
