using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CiftciApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CiftciApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Messages/Conversation/1/2
        [HttpGet("Conversation/{userId1}/{userId2}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversation(int userId1, int userId2)
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Product)
                .Where(m =>
                    (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                    (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender.Name,
                SenderType = m.Sender.UserType,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver.Name,
                ReceiverType = m.Receiver.UserType,
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt,
                ReadAt = m.ReadAt,
                ProductId = m.ProductId,
                ProductName = m.Product?.Name
            }).ToList();
        }

        // GET: api/Messages/User/1
        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<MessageGroupDto>>> GetUserMessages(int userId)
        {
            // Tüm alınan ve gönderilen mesajları al
            var allMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Konuşmaları grupla
            var conversationGroups = new Dictionary<int, MessageGroupDto>();

            foreach (var message in allMessages)
            {
                int otherUserId = message.SenderId == userId ? message.ReceiverId : message.SenderId;
                var otherUser = message.SenderId == userId ? message.Receiver : message.Sender;

                if (!conversationGroups.ContainsKey(otherUserId))
                {
                    conversationGroups[otherUserId] = new MessageGroupDto
                    {
                        UserId = otherUserId,
                        UserName = otherUser.Name,
                        UserType = otherUser.UserType,
                        UserImage = otherUser.ProfileImageUrl,
                        LastMessage = message.Content,
                        LastMessageDate = message.CreatedAt,
                        UnreadCount = message.ReceiverId == userId && !message.IsRead ? 1 : 0
                    };
                }
                else if (message.CreatedAt > conversationGroups[otherUserId].LastMessageDate)
                {
                    // Son mesajı güncelle
                    conversationGroups[otherUserId].LastMessage = message.Content;
                    conversationGroups[otherUserId].LastMessageDate = message.CreatedAt;
                }

                // Okunmamış mesaj sayısı
                if (message.ReceiverId == userId && !message.IsRead)
                {
                    conversationGroups[otherUserId].UnreadCount++;
                }
            }

            return conversationGroups.Values.OrderByDescending(g => g.LastMessageDate).ToList();
        }

        // POST: api/Messages
        [HttpPost]
        public async Task<ActionResult<Message>> SendMessage(CreateMessageModel model)
        {
            var sender = await _context.Users.FindAsync(model.SenderId);
            if (sender == null)
            {
                return BadRequest("Gönderen kullanıcı bulunamadı.");
            }

            var receiver = await _context.Users.FindAsync(model.ReceiverId);
            if (receiver == null)
            {
                return BadRequest("Alıcı kullanıcı bulunamadı.");
            }

            // Ürün kontrolü (opsiyonel)
            if (model.ProductId.HasValue)
            {
                var product = await _context.Products.FindAsync(model.ProductId.Value);
                if (product == null)
                {
                    return BadRequest("Belirtilen ürün bulunamadı.");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var message = new Message
                {
                    SenderId = model.SenderId,
                    ReceiverId = model.ReceiverId,
                    Content = model.Content,
                    ProductId = model.ProductId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Messages.Add(message);

                // Bildirim ekle
                var notification = new Notification
                {
                    UserId = model.ReceiverId,
                    Title = "Yeni Mesaj",
                    Content = $"{sender.Name} size yeni bir mesaj gönderdi: {(model.Content.Length > 30 ? model.Content.Substring(0, 30) + "..." : model.Content)}",
                    Type = NotificationType.NewMessage,
                    IsRead = false,
                    RelatedEntityId = model.SenderId,
                    CreatedAt = DateTime.Now,
                    RedirectUrl = $"/messages/{model.SenderId}"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return CreatedAtAction("GetConversation", new { userId1 = model.SenderId, userId2 = model.ReceiverId }, message);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PUT: api/Messages/MarkAsRead/5
        [HttpPut("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkMessageAsRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // PUT: api/Messages/MarkAllAsRead/1/2
        [HttpPut("MarkAllAsRead/{receiverId}/{senderId}")]
        public async Task<IActionResult> MarkAllMessagesAsRead(int receiverId, int senderId)
        {
            var messages = await _context.Messages
                .Where(m => m.ReceiverId == receiverId && m.SenderId == senderId && !m.IsRead)
                .ToListAsync();

            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // GET: api/Messages/UnreadCount/1
        [HttpGet("UnreadCount/{userId}")]
        public async Task<ActionResult<int>> GetUnreadMessageCount(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        }
    }

    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public UserType SenderType { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public UserType ReceiverType { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; }
    }

    public class MessageGroupDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public UserType UserType { get; set; }
        public string UserImage { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }

    public class CreateMessageModel
    {
        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required, StringLength(1000)]
        public string Content { get; set; }

        public int? ProductId { get; set; }
    }
}