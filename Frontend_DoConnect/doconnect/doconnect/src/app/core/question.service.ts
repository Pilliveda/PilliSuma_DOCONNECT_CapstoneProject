// import { Injectable } from '@angular/core';
// import { HttpClient, HttpParams } from '@angular/common/http';
// import { environment } from '../../environments/environment';

// // Adjust these interfaces to your actual API shapes if needed
// export interface QuestionDto {
//   id: string;
//   title: string;
//   text: string;
//   author?: string;
//   createdAt?: string;
// }

// export interface CreateQuestionDto {
//   title: string;
//   body: string;
// }

// export interface AnswerDto {
//   id: string;
//   text: string;
//   author?: string;
//   createdAt?: string;
// }

// @Injectable({ providedIn: 'root' })
// export class QuestionService {
//   private api = `${environment.apiUrl}/questions`; // base

//   constructor(private http: HttpClient) {}

//   getQuestions(search = '', page = 1, pageSize = 20) {
//     let params = new HttpParams()
//       .set('page', page.toString())
//       .set('pageSize', pageSize.toString());

//     if (search) params = params.set('q', search);

//     return this.http.get<{ items: QuestionDto[]; total: number }>(this.api, { params });
//   }

//   getQuestion(id: string) {
//     return this.http.get<QuestionDto>(`${this.api}/${id}`);
//   }

//   createQuestion(dto: CreateQuestionDto, files?: File[]) {
//     const fd = new FormData();
//     fd.append('Title', dto.title);   // backend expects "Title"
//     fd.append('Text', dto.body);     // backend expects "Text"
//     if (files?.length) files.forEach(f => fd.append('Files', f, f.name)); // backend expects "Files"
//     return this.http.post<QuestionDto>(this.api, fd);
//   }

//   // NOTE: backend path uses /Answers (per your Swagger)
//   getAnswers(questionId: string) {
//     return this.http.get<AnswerDto[]>(`${this.api}/${questionId}/Answers`);
//   }

//   // Must send multipart/form-data with keys Text and Files
//   postAnswer(questionId: string, formData: FormData) {
//     return this.http.post<AnswerDto>(`${this.api}/${questionId}/Answers`, formData);
//   }
// }


import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface QuestionDto {
  id: string;
  title: string;
  text: string;
  author?: string;
  createdAt?: string;
  images?: string[];        // <— add
}

export interface AnswerDto {
  id: string;
  text: string;
  author?: string;
  createdAt?: string;
  status?: string;
  images?: string[];        // <— add
}

export interface CreateQuestionDto { title: string; body: string; }
// export interface AnswerDto {
//   id: string; text: string; author?: string; createdAt?: string; status?: string;
// }

@Injectable({ providedIn: 'root' })
export class QuestionService {
  private api = `${environment.apiUrl}/questions`;
  constructor(private http: HttpClient) {}

  getQuestions(search = '', page = 1, pageSize = 20) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('q', search);
    return this.http.get<{ items: QuestionDto[]; total: number }>(this.api, { params });
  }

  getQuestion(id: string) {
    return this.http.get<QuestionDto>(`${this.api}/${id}`);
  }

  createQuestion(dto: CreateQuestionDto, files?: File[]) {
    const fd = new FormData();
    fd.append('Title', dto.title);
    fd.append('Text', dto.body);
    files?.forEach(f => fd.append('Files', f, f.name));
    return this.http.post<QuestionDto>(this.api, fd);
  }

  getAnswers(questionId: string) {
    return this.http.get<AnswerDto[]>(`${this.api}/${questionId}/Answers`);
  }

  // Send multipart/form-data for user answers (pending by default)
  postAnswer(questionId: string, formData: FormData) {
    return this.http.post<AnswerDto>(`${this.api}/${questionId}/Answers`, formData);
  }

  // Convenience helper (optional)
  buildAnswerForm(text: string, files?: File[]) {
    const fd = new FormData();
    fd.append('Text', text);
    files?.forEach(f => fd.append('Files', f, f.name));
    return fd;
  }
}
