# Email Bugtracker

> Email -> Sendgrid -> Azure function -> Azure DevOps Bug

Write an email:

![email](images/email.png)

and have it turn into a bug in Azure DevOps automagically:

![bug](images/bug.png)

# Motivation

This project provides an azure function to convert incoming emails into Azure DevOps bugs.

I wrote this

* to reduce complexity (both setting up and running it) compared to mail2bug
* as a learning expirience for myself

## Requirements

It requires a valid email domain and a sendgrid account (if you don't have one, Azure provides a suitable free tier).

If you are looking for a more feature complete version, look no further than [mail2bug](https://github.com/microsoft/mail2bug).

# How it works

This relies on the Sendgrid Webhooks. Specifically its [Inbound Parse](https://sendgrid.com/docs/for-developers/parsing-email/inbound-email/) feature.

`Inbound Parse` will route all mails received at the domain through sendgrid and also sends them to the azure function.

Based on the configuration the azure function then ignores or processes the emails and creates bugs in Azure DevOps when necessary.

# How to deploy

See [Setup.md](Setup.md)
