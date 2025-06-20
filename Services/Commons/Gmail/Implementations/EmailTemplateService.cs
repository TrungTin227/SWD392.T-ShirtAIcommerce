using DTOs.EmailDTOs;

namespace Services.Commons.Gmail.Implementations
{
    public class EmailTemplateService
    {
        public static EmailTemplate GetWelcomeTemplate(string firstName, string lastName)
        {
            return new EmailTemplate
            {
                Type = EmailTemplateType.Welcome,
                Subject = "Chào mừng bạn đến với T-Shirt AI Commerce!",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #2c3e50;'>T-Shirt AI Commerce</h1>
                            </div>
                            
                            <h2 style='color: #3498db;'>Xin chào {firstName} {lastName}!</h2>
                            
                            <p>Chào mừng bạn đến với T-Shirt AI Commerce - nền tảng thiết kế áo thun thông minh hàng đầu!</p>
                            
                            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <h3 style='color: #2c3e50; margin-top: 0;'>Những tính năng tuyệt vời đang chờ bạn:</h3>
                                <ul>
                                    <li>🎨 Thiết kế áo thun với AI</li>
                                    <li>🛒 Mua sắm dễ dàng và thuận tiện</li>
                                    <li>📦 Giao hàng nhanh chóng</li>
                                    <li>💡 Tạo design độc đáo theo ý tưởng riêng</li>
                                </ul>
                            </div>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='#' style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Bắt đầu mua sắm ngay
                                </a>
                            </div>
                            
                            <p>Cảm ơn bạn đã tin tương và lựa chọn chúng tôi!</p>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            
                            <div style='text-align: center; color: #666; font-size: 12px;'>
                                <p>T-Shirt AI Commerce Team<br>
                                Email: support@tshirtai.com | Hotline: 1900-xxxx</p>
                            </div>
                        </div>
                    </body>
                    </html>"
            };
        }

        public static EmailTemplate GetEmailConfirmationTemplate(string firstName, string confirmationLink)
        {
            return new EmailTemplate
            {
                Type = EmailTemplateType.EmailConfirmation,
                Subject = "Xác nhận địa chỉ email của bạn",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #2c3e50;'>T-Shirt AI Commerce</h1>
                            </div>
                            
                            <h2 style='color: #3498db;'>Xin chào {firstName}!</h2>
                            
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại T-Shirt AI Commerce.</p>
                            <p>Để hoàn tất quá trình đăng ký, vui lòng xác nhận địa chỉ email của bạn bằng cách nhấn vào nút bên dưới:</p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{confirmationLink}' style='background-color: #27ae60; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>
                                    Xác nhận Email
                                </a>
                            </div>
                            
                            <p>Hoặc copy và paste link sau vào trình duyệt:</p>
                            <p style='background-color: #f8f9fa; padding: 10px; border-radius: 4px; word-break: break-all;'>{confirmationLink}</p>
                            
                            <p><strong>Lưu ý:</strong> Link xác nhận này có hiệu lực trong 24 giờ.</p>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            
                            <div style='text-align: center; color: #666; font-size: 12px;'>
                                <p>Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.</p>
                                <p>T-Shirt AI Commerce Team</p>
                            </div>
                        </div>
                    </body>
                    </html>"
            };
        }

        public static EmailTemplate GetPasswordResetTemplate(string firstName, string resetLink)
        {
            return new EmailTemplate
            {
                Type = EmailTemplateType.PasswordReset,
                Subject = "Đặt lại mật khẩu tài khoản",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #2c3e50;'>T-Shirt AI Commerce</h1>
                            </div>
                            
                            <h2 style='color: #e74c3c;'>Đặt lại mật khẩu</h2>
                            
                            <p>Xin chào {firstName},</p>
                            
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' style='background-color: #e74c3c; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;'>
                                    Đặt lại mật khẩu
                                </a>
                            </div>
                            
                            <p>Hoặc copy và paste link sau vào trình duyệt:</p>
                            <p style='background-color: #f8f9fa; padding: 10px; border-radius: 4px; word-break: break-all;'>{resetLink}</p>
                            
                            <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0; color: #856404;'><strong>Bảo mật:</strong> Link này có hiệu lực trong 1 giờ và chỉ sử dụng được 1 lần.</p>
                            </div>
                            
                            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này và mật khẩu của bạn sẽ không thay đổi.</p>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            
                            <div style='text-align: center; color: #666; font-size: 12px;'>
                                <p>T-Shirt AI Commerce Team<br>
                                Email: support@tshirtai.com</p>
                            </div>
                        </div>
                    </body>
                    </html>"
            };
        }

        public static EmailTemplate GetOrderConfirmationTemplate(string firstName, string orderNumber, decimal amount)
        {
            return new EmailTemplate
            {
                Type = EmailTemplateType.OrderConfirmation,
                Subject = $"Xác nhận đơn hàng #{orderNumber}",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #2c3e50;'>T-Shirt AI Commerce</h1>
                            </div>
                            
                            <h2 style='color: #27ae60;'>✅ Đơn hàng đã được xác nhận!</h2>
                            
                            <p>Xin chào {firstName},</p>
                            
                            <p>Cảm ơn bạn đã mua sắm tại T-Shirt AI Commerce! Đơn hàng của bạn đã được xác nhận thành công.</p>
                            
                            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <h3 style='margin-top: 0; color: #2c3e50;'>Thông tin đơn hàng:</h3>
                                <p><strong>Mã đơn hàng:</strong> #{orderNumber}</p>
                                <p><strong>Tổng tiền:</strong> {amount:C}</p>
                                <p><strong>Ngày đặt:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                            </div>
                            
                            <p>Chúng tôi đang xử lý đơn hàng của bạn và sẽ gửi thông báo khi đơn hàng được giao cho đơn vị vận chuyển.</p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='#' style='background-color: #3498db; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block; margin-right: 10px;'>
                                    Theo dõi đơn hàng
                                </a>
                                <a href='#' style='background-color: #95a5a6; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Liên hệ hỗ trợ
                                </a>
                            </div>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            
                            <div style='text-align: center; color: #666; font-size: 12px;'>
                                <p>T-Shirt AI Commerce Team<br>
                                Email: support@tshirtai.com | Hotline: 1900-xxxx</p>
                            </div>
                        </div>
                    </body>
                    </html>"
            };
        }
    }
}