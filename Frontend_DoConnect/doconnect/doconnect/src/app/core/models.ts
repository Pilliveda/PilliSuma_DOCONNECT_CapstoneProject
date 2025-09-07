// src/app/core/models.ts
export interface User { id?: number; name?: string; email?: string; role?: string; }
export interface AuthResponse { token: string; expiresIn?: number; user: User; }
// Login
export interface LoginDto {
  usernameOrEmail: string;
  password: string;
}

// Register
export interface RegisterDto {
  username: string;
  email: string;
  password: string;
}
export interface QuestionDto {
  id: number; title: string; body: string; createdBy?: User | null;
  status?: 'pending'|'approved'|'rejected'; createdAt?: string; images?: string[];
}
export interface CreateQuestionDto {
  title: string;  // maps to "Title"
  body: string;   // maps to "Text"
}

export interface AnswerDto { id: number; questionId: number; body: string; createdBy?: User; status?: string; createdAt?: string;}
