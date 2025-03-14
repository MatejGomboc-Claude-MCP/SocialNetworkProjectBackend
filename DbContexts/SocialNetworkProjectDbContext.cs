using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialNetworkProjectBackend.DbContexts
{
    public class SocialNetworkProjectDbContext : DbContext
    {
        public class User
        {
            public static string HashPassword(string password)
            {
                return Argon2.Hash(password);
            }

            public static bool VerifyPassword(string password, string passwordHash)
            {
                return Argon2.Verify(passwordHash, password);
            }

            public static string HashRefreshToken(string refreshToken)
            {
                return Argon2.Hash(refreshToken);
            }

            public static bool VerifyRefreshToken(string refreshToken, string refreshTokenHash)
            {
                return Argon2.Verify(refreshTokenHash, refreshToken);
            }

            [Key]
            [MaxLength(128)]
            [Required(AllowEmptyStrings = false)]
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string Username { get; set; } = string.Empty;

            [Required(AllowEmptyStrings = false)]
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string PasswordHash { get; set; } = string.Empty;

            [DataType(DataType.EmailAddress)]
            [MaxLength(128)]
            [Required(AllowEmptyStrings = false)]
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string EmailAddress { get; set; } = string.Empty;

            [Required]
            public bool EmailAddressConfirmed { get; set; } = false;

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime DateTimeRegistered { get; set; } = DateTime.UnixEpoch;

            [Required(AllowEmptyStrings = false)]
            [DisplayFormat(ConvertEmptyStringToNull = false)]
            public string RefreshTokenHash { get; set; } = string.Empty;

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime DateTimeRefreshTokenCreated { get; set; } = DateTime.UnixEpoch;
            
            // Navigation properties
            public virtual UserProfile? Profile { get; set; }
            public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
            public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
            public virtual ICollection<FriendRequest> SentFriendRequests { get; set; } = new List<FriendRequest>();
            public virtual ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = new List<FriendRequest>();
            public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
        }

        public class UserProfile
        {
            [Key]
            [ForeignKey("User")]
            [MaxLength(128)]
            public string Username { get; set; } = string.Empty;

            [MaxLength(100)]
            public string? DisplayName { get; set; }

            [MaxLength(500)]
            public string? Bio { get; set; }

            [MaxLength(255)]
            public string? ProfilePictureUrl { get; set; }

            [MaxLength(100)]
            public string? Location { get; set; }

            public DateTime? BirthDate { get; set; }

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

            // Navigation property
            public virtual User User { get; set; } = null!;
        }

        public class FriendRequest
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            [MaxLength(128)]
            [ForeignKey("Sender")]
            public string SenderUsername { get; set; } = string.Empty;

            [Required]
            [MaxLength(128)]
            [ForeignKey("Receiver")]
            public string ReceiverUsername { get; set; } = string.Empty;

            [Required]
            public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            [DataType(DataType.DateTime)]
            public DateTime? RespondedAt { get; set; }

            // Navigation properties
            public virtual User Sender { get; set; } = null!;
            public virtual User Receiver { get; set; } = null!;
        }

        public enum FriendRequestStatus
        {
            Pending,
            Accepted,
            Rejected
        }

        public class Post
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            [MaxLength(128)]
            [ForeignKey("Author")]
            public string AuthorUsername { get; set; } = string.Empty;

            [Required]
            [MaxLength(2000)]
            public string Content { get; set; } = string.Empty;

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            [DataType(DataType.DateTime)]
            public DateTime? UpdatedAt { get; set; }

            [Required]
            public PostVisibility Visibility { get; set; } = PostVisibility.Public;

            // Navigation properties
            public virtual User Author { get; set; } = null!;
            public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
            public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
        }

        public enum PostVisibility
        {
            Public,
            Friends,
            Private
        }

        public class Comment
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            [ForeignKey("Post")]
            public Guid PostId { get; set; }

            [Required]
            [MaxLength(128)]
            [ForeignKey("Author")]
            public string AuthorUsername { get; set; } = string.Empty;

            [Required]
            [MaxLength(1000)]
            public string Content { get; set; } = string.Empty;

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            [DataType(DataType.DateTime)]
            public DateTime? UpdatedAt { get; set; }

            // For nested comments (replies)
            public Guid? ParentCommentId { get; set; }

            // Navigation properties
            public virtual Post Post { get; set; } = null!;
            public virtual User Author { get; set; } = null!;
            
            [ForeignKey("ParentCommentId")]
            public virtual Comment? ParentComment { get; set; }
            
            public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
        }

        public class PostLike
        {
            [Key]
            public Guid Id { get; set; }

            [Required]
            [ForeignKey("Post")]
            public Guid PostId { get; set; }

            [Required]
            [MaxLength(128)]
            [ForeignKey("User")]
            public string Username { get; set; } = string.Empty;

            [DataType(DataType.DateTime)]
            [Required]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // Navigation properties
            public virtual Post Post { get; set; } = null!;
            public virtual User User { get; set; } = null!;
        }

        public SocialNetworkProjectDbContext(DbContextOptions<SocialNetworkProjectDbContext> options) :
            base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure friendship relationship
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(fr => fr.SenderUsername)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(fr => fr.ReceiverUsername)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent self-friendship
            modelBuilder.Entity<FriendRequest>()
                .HasCheckConstraint("CK_FriendRequest_NoSelfFriendship", "\"SenderUsername\" <> \"ReceiverUsername\"");

            // Unique likes (one user can like a post only once)
            modelBuilder.Entity<PostLike>()
                .HasIndex(pl => new { pl.PostId, pl.Username })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<User>? Users { get; set; }
        public DbSet<UserProfile>? UserProfiles { get; set; }
        public DbSet<FriendRequest>? FriendRequests { get; set; }
        public DbSet<Post>? Posts { get; set; }
        public DbSet<Comment>? Comments { get; set; }
        public DbSet<PostLike>? PostLikes { get; set; }
    }
}
