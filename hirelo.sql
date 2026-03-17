--
-- PostgreSQL database dump
--

\restrict SukGw0HjvpuJS9cULLIsx05imVZeZhwMSlohiBfxSdeAmd4h6ymk6AXdKWSjEnL

-- Dumped from database version 18.2
-- Dumped by pg_dump version 18.2

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: CandidateProfiles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CandidateProfiles" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Seniority" text
);


ALTER TABLE public."CandidateProfiles" OWNER TO postgres;

--
-- Name: CodeEvaluations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CodeEvaluations" (
    "Id" uuid NOT NULL,
    "SubmissionId" uuid NOT NULL,
    "Passed" boolean NOT NULL,
    "RuntimeMs" integer,
    "MemoryKb" integer
);


ALTER TABLE public."CodeEvaluations" OWNER TO postgres;

--
-- Name: CodeSubmissions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."CodeSubmissions" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "Language" text,
    "SourceCode" text,
    "SubmittedAt" timestamp with time zone
);


ALTER TABLE public."CodeSubmissions" OWNER TO postgres;

--
-- Name: Companies; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Companies" (
    "Id" uuid NOT NULL,
    "Name" text,
    "CreatedAt" timestamp with time zone
);


ALTER TABLE public."Companies" OWNER TO postgres;

--
-- Name: EmotionFrames; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."EmotionFrames" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "Emotion" text,
    "Confidence" double precision NOT NULL,
    "CapturedAt" timestamp with time zone
);


ALTER TABLE public."EmotionFrames" OWNER TO postgres;

--
-- Name: InterviewAnswers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."InterviewAnswers" (
    "Id" uuid NOT NULL,
    "QuestionId" uuid NOT NULL,
    "Transcript" text,
    "AudioUrl" text
);


ALTER TABLE public."InterviewAnswers" OWNER TO postgres;

--
-- Name: InterviewQuestions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."InterviewQuestions" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "QuestionText" text,
    "CreatedAt" timestamp with time zone
);


ALTER TABLE public."InterviewQuestions" OWNER TO postgres;

--
-- Name: InterviewReports; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."InterviewReports" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "ReportJson" text,
    "ReportHtml" text
);


ALTER TABLE public."InterviewReports" OWNER TO postgres;

--
-- Name: InterviewSessions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."InterviewSessions" (
    "Id" uuid NOT NULL,
    "JobId" uuid NOT NULL,
    "CandidateId" uuid NOT NULL,
    "Status" text,
    "StartedAt" timestamp with time zone,
    "EndedAt" timestamp with time zone
);


ALTER TABLE public."InterviewSessions" OWNER TO postgres;

--
-- Name: Jobs; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Jobs" (
    "Id" uuid NOT NULL,
    "RecruiterId" uuid NOT NULL,
    "Title" text,
    "Description" text,
    "CreatedAt" timestamp with time zone
);


ALTER TABLE public."Jobs" OWNER TO postgres;

--
-- Name: Notifications; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Notifications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" text,
    "Message" text,
    "CreatedAt" timestamp with time zone
);


ALTER TABLE public."Notifications" OWNER TO postgres;

--
-- Name: RecruiterProfiles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RecruiterProfiles" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CompanyId" uuid NOT NULL
);


ALTER TABLE public."RecruiterProfiles" OWNER TO postgres;

--
-- Name: Scorecards; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Scorecards" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "TechnicalScore" double precision NOT NULL,
    "CommunicationScore" double precision NOT NULL,
    "ProblemSolvingScore" double precision NOT NULL,
    "OverallScore" double precision NOT NULL
);


ALTER TABLE public."Scorecards" OWNER TO postgres;

--
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "CognitoSub" text,
    "Email" text NOT NULL,
    "Role" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


ALTER TABLE public."__EFMigrationsHistory" OWNER TO postgres;

--
-- Data for Name: CandidateProfiles; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."CandidateProfiles" ("Id", "UserId", "Seniority") FROM stdin;
\.


--
-- Data for Name: CodeEvaluations; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."CodeEvaluations" ("Id", "SubmissionId", "Passed", "RuntimeMs", "MemoryKb") FROM stdin;
\.


--
-- Data for Name: CodeSubmissions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."CodeSubmissions" ("Id", "SessionId", "Language", "SourceCode", "SubmittedAt") FROM stdin;
\.


--
-- Data for Name: Companies; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Companies" ("Id", "Name", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: EmotionFrames; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."EmotionFrames" ("Id", "SessionId", "Emotion", "Confidence", "CapturedAt") FROM stdin;
\.


--
-- Data for Name: InterviewAnswers; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."InterviewAnswers" ("Id", "QuestionId", "Transcript", "AudioUrl") FROM stdin;
\.


--
-- Data for Name: InterviewQuestions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."InterviewQuestions" ("Id", "SessionId", "QuestionText", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: InterviewReports; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."InterviewReports" ("Id", "SessionId", "ReportJson", "ReportHtml") FROM stdin;
\.


--
-- Data for Name: InterviewSessions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."InterviewSessions" ("Id", "JobId", "CandidateId", "Status", "StartedAt", "EndedAt") FROM stdin;
\.


--
-- Data for Name: Jobs; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Jobs" ("Id", "RecruiterId", "Title", "Description", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: Notifications; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Notifications" ("Id", "UserId", "Type", "Message", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: RecruiterProfiles; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."RecruiterProfiles" ("Id", "UserId", "CompanyId") FROM stdin;
\.


--
-- Data for Name: Scorecards; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Scorecards" ("Id", "SessionId", "TechnicalScore", "CommunicationScore", "ProblemSolvingScore", "OverallScore") FROM stdin;
\.


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Users" ("Id", "CognitoSub", "Email", "Role", "CreatedAt") FROM stdin;
dbb0bfe2-0235-4046-ace5-2d5a47f5bfc5	f99af58c-b0f1-70ab-e97c-db8e5fe087af	iky36412@laoia.com	Candidate	2026-03-02 18:00:01.435468+07
ba95f787-96e0-40cf-9ca3-ee7998a02e99	29aad5ac-20c1-70ce-a095-21f5a5727181	dtr39804@laoia.com	Candidate	2026-03-02 20:34:17.194449+07
821d5622-5a39-4536-9f15-3fc9888203fd	193af55c-9061-70a5-5359-dc226ec24637	yoh13973@laoia.com	Candidate	2026-03-02 20:42:33.683046+07
40b5a62c-5270-48f3-9c19-421b352bb76d	691ac55c-10e1-7017-563d-beccaf6e4508	gui46567@laoia.com	Candidate	2026-03-02 22:26:56.888586+07
60e14c1c-e90c-48f6-baf9-57d10bcc1e0b	799af5cc-c041-70f4-9ba8-f97ae148880d	fxj62967@laoia.com	Candidate	2026-03-02 22:30:14.674178+07
d8eeb9b5-ddcf-475b-a95d-53ee8a79aaf7	095a755c-e051-703e-3549-007fb9fedd6a	ija41039@laoia.com	Candidate	2026-03-02 22:30:51.425549+07
5b2a8510-906c-484c-8063-3272122ae17f	09ca25ac-d031-707b-6f1b-4b36b31bb6ad	sva88175@laoia.com	Candidate	2026-03-03 10:56:39.374163+07
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260228183243_InitialCreate	8.0.24
\.


--
-- Name: CandidateProfiles PK_CandidateProfiles; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CandidateProfiles"
    ADD CONSTRAINT "PK_CandidateProfiles" PRIMARY KEY ("Id");


--
-- Name: CodeEvaluations PK_CodeEvaluations; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CodeEvaluations"
    ADD CONSTRAINT "PK_CodeEvaluations" PRIMARY KEY ("Id");


--
-- Name: CodeSubmissions PK_CodeSubmissions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CodeSubmissions"
    ADD CONSTRAINT "PK_CodeSubmissions" PRIMARY KEY ("Id");


--
-- Name: Companies PK_Companies; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Companies"
    ADD CONSTRAINT "PK_Companies" PRIMARY KEY ("Id");


--
-- Name: EmotionFrames PK_EmotionFrames; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EmotionFrames"
    ADD CONSTRAINT "PK_EmotionFrames" PRIMARY KEY ("Id");


--
-- Name: InterviewAnswers PK_InterviewAnswers; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewAnswers"
    ADD CONSTRAINT "PK_InterviewAnswers" PRIMARY KEY ("Id");


--
-- Name: InterviewQuestions PK_InterviewQuestions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewQuestions"
    ADD CONSTRAINT "PK_InterviewQuestions" PRIMARY KEY ("Id");


--
-- Name: InterviewReports PK_InterviewReports; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewReports"
    ADD CONSTRAINT "PK_InterviewReports" PRIMARY KEY ("Id");


--
-- Name: InterviewSessions PK_InterviewSessions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewSessions"
    ADD CONSTRAINT "PK_InterviewSessions" PRIMARY KEY ("Id");


--
-- Name: Jobs PK_Jobs; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Jobs"
    ADD CONSTRAINT "PK_Jobs" PRIMARY KEY ("Id");


--
-- Name: Notifications PK_Notifications; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Notifications"
    ADD CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id");


--
-- Name: RecruiterProfiles PK_RecruiterProfiles; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RecruiterProfiles"
    ADD CONSTRAINT "PK_RecruiterProfiles" PRIMARY KEY ("Id");


--
-- Name: Scorecards PK_Scorecards; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Scorecards"
    ADD CONSTRAINT "PK_Scorecards" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_CandidateProfiles_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CandidateProfiles_UserId" ON public."CandidateProfiles" USING btree ("UserId");


--
-- Name: IX_CodeEvaluations_SubmissionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_CodeEvaluations_SubmissionId" ON public."CodeEvaluations" USING btree ("SubmissionId");


--
-- Name: IX_CodeSubmissions_SessionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_CodeSubmissions_SessionId" ON public."CodeSubmissions" USING btree ("SessionId");


--
-- Name: IX_EmotionFrames_SessionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_EmotionFrames_SessionId" ON public."EmotionFrames" USING btree ("SessionId");


--
-- Name: IX_InterviewAnswers_QuestionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InterviewAnswers_QuestionId" ON public."InterviewAnswers" USING btree ("QuestionId");


--
-- Name: IX_InterviewQuestions_SessionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InterviewQuestions_SessionId" ON public."InterviewQuestions" USING btree ("SessionId");


--
-- Name: IX_InterviewReports_SessionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_InterviewReports_SessionId" ON public."InterviewReports" USING btree ("SessionId");


--
-- Name: IX_InterviewSessions_CandidateId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InterviewSessions_CandidateId" ON public."InterviewSessions" USING btree ("CandidateId");


--
-- Name: IX_InterviewSessions_JobId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_InterviewSessions_JobId" ON public."InterviewSessions" USING btree ("JobId");


--
-- Name: IX_Jobs_RecruiterId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Jobs_RecruiterId" ON public."Jobs" USING btree ("RecruiterId");


--
-- Name: IX_Notifications_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Notifications_UserId" ON public."Notifications" USING btree ("UserId");


--
-- Name: IX_RecruiterProfiles_CompanyId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_RecruiterProfiles_CompanyId" ON public."RecruiterProfiles" USING btree ("CompanyId");


--
-- Name: IX_RecruiterProfiles_UserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_RecruiterProfiles_UserId" ON public."RecruiterProfiles" USING btree ("UserId");


--
-- Name: IX_Scorecards_SessionId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Scorecards_SessionId" ON public."Scorecards" USING btree ("SessionId");


--
-- Name: CandidateProfiles FK_CandidateProfiles_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CandidateProfiles"
    ADD CONSTRAINT "FK_CandidateProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: CodeEvaluations FK_CodeEvaluations_CodeSubmissions_SubmissionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CodeEvaluations"
    ADD CONSTRAINT "FK_CodeEvaluations_CodeSubmissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES public."CodeSubmissions"("Id") ON DELETE CASCADE;


--
-- Name: CodeSubmissions FK_CodeSubmissions_InterviewSessions_SessionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."CodeSubmissions"
    ADD CONSTRAINT "FK_CodeSubmissions_InterviewSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES public."InterviewSessions"("Id") ON DELETE CASCADE;


--
-- Name: EmotionFrames FK_EmotionFrames_InterviewSessions_SessionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."EmotionFrames"
    ADD CONSTRAINT "FK_EmotionFrames_InterviewSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES public."InterviewSessions"("Id") ON DELETE CASCADE;


--
-- Name: InterviewAnswers FK_InterviewAnswers_InterviewQuestions_QuestionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewAnswers"
    ADD CONSTRAINT "FK_InterviewAnswers_InterviewQuestions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES public."InterviewQuestions"("Id") ON DELETE CASCADE;


--
-- Name: InterviewQuestions FK_InterviewQuestions_InterviewSessions_SessionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewQuestions"
    ADD CONSTRAINT "FK_InterviewQuestions_InterviewSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES public."InterviewSessions"("Id") ON DELETE CASCADE;


--
-- Name: InterviewReports FK_InterviewReports_InterviewSessions_SessionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewReports"
    ADD CONSTRAINT "FK_InterviewReports_InterviewSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES public."InterviewSessions"("Id") ON DELETE CASCADE;


--
-- Name: InterviewSessions FK_InterviewSessions_CandidateProfiles_CandidateId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewSessions"
    ADD CONSTRAINT "FK_InterviewSessions_CandidateProfiles_CandidateId" FOREIGN KEY ("CandidateId") REFERENCES public."CandidateProfiles"("Id") ON DELETE CASCADE;


--
-- Name: InterviewSessions FK_InterviewSessions_Jobs_JobId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."InterviewSessions"
    ADD CONSTRAINT "FK_InterviewSessions_Jobs_JobId" FOREIGN KEY ("JobId") REFERENCES public."Jobs"("Id") ON DELETE CASCADE;


--
-- Name: Jobs FK_Jobs_RecruiterProfiles_RecruiterId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Jobs"
    ADD CONSTRAINT "FK_Jobs_RecruiterProfiles_RecruiterId" FOREIGN KEY ("RecruiterId") REFERENCES public."RecruiterProfiles"("Id") ON DELETE CASCADE;


--
-- Name: Notifications FK_Notifications_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Notifications"
    ADD CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: RecruiterProfiles FK_RecruiterProfiles_Companies_CompanyId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RecruiterProfiles"
    ADD CONSTRAINT "FK_RecruiterProfiles_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES public."Companies"("Id") ON DELETE CASCADE;


--
-- Name: RecruiterProfiles FK_RecruiterProfiles_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RecruiterProfiles"
    ADD CONSTRAINT "FK_RecruiterProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Scorecards FK_Scorecards_InterviewSessions_SessionId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Scorecards"
    ADD CONSTRAINT "FK_Scorecards_InterviewSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES public."InterviewSessions"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict SukGw0HjvpuJS9cULLIsx05imVZeZhwMSlohiBfxSdeAmd4h6ymk6AXdKWSjEnL

