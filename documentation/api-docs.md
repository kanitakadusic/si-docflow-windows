# docflow-windows

## API documentation

### Capture, Process, Send

> This endpoint is active while the Windows application is running in headless mode. Calling it captures the document, sends it to the processing server for OCR, and then submits it to the admin server.

- _Method:_ POST

- _URL:_ http(s)://[localhost:8080](./appsettings-setup.md#machine_id-)/process

- _Body:_ raw + JSON
```json
{
    "transaction_id": 1,
    "document_type_id": 1,
    "file_name": "document_name.png"
}
```
