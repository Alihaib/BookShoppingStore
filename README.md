# 📚 Bookshopping Store (.NET)

This is a C#/.NET-based **online bookshopping store** that allows users to browse a catalog of books, add them to a cart, and complete purchases using **PayPal**. The system includes user authentication, admin-side book management, and was also used as a **security lab project** to demonstrate SQL injection vulnerabilities.

---

## 🚀 Key Features

- 🛒 Add books to a shopping cart
- 💳 Pay securely using PayPal (sandbox integration)
- 👤 User login and registration
- 🧑‍💼 Admin panel for adding/editing/removing books
- 🔍 Search functionality (with intentionally insecure SQL for lab testing)
- ⚠️ SQL Injection testing support for educational purposes

---

## 🔒 Security Focus

This project was part of a **security lab** focused on demonstrating and testing:

- **SQL Injection**
- **Improper input sanitization**
- Vulnerabilities in login and search functionality

**Note:** Vulnerabilities were intentionally left in for testing purposes and should be mitigated before deploying in production.

---

## 🧪 SQL Injection Example

**Vulnerable Query Example:**
```csharp
string query = $"SELECT * FROM Users WHERE Username = '{username}' AND Password = '{password}'";
