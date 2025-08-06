import os
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional, Union, Set

from fastapi import FastAPI, Query, WebSocket, WebSocketDisconnect
from fastapi.responses import HTMLResponse
from pydantic import BaseModel

from ibind import IbkrClient, ibind_logs_initialize

ibind_logs_initialize()

client = IbkrClient(
    cacert=os.getenv('IBIND_CACERT', False),
    use_oauth=True,
)

app = FastAPI()
_connections: Set[WebSocket] = set()


def _monitor_page() -> str:
    return Path(__file__).with_name('monitor.html').read_text()


async def _broadcast(message: Dict[str, Any]) -> None:
    dead: List[WebSocket] = []
    for ws in _connections:
        try:
            await ws.send_json(message)
        except Exception:
            dead.append(ws)
    for ws in dead:
        _connections.discard(ws)


async def _send_event(endpoint: str, data: Any) -> None:
    await _broadcast({'time': datetime.utcnow().isoformat(), 'endpoint': endpoint, 'data': data})


@app.on_event('startup')
async def startup_event() -> None:
    accounts = client.portfolio_accounts().data
    if accounts:
        client.account_id = accounts[0].get('accountId')
    client.receive_brokerage_accounts()
    await _send_event('startup', accounts)


@app.get('/monitor', response_class=HTMLResponse)
async def monitor_page() -> str:
    return _monitor_page()


@app.websocket('/ws')
async def websocket_feed(ws: WebSocket) -> None:
    await ws.accept()
    _connections.add(ws)
    try:
        while True:
            await ws.receive_text()
    except WebSocketDisconnect:
        _connections.discard(ws)


@app.get('/health')
async def get_health() -> Any:
    status = {
        'gateway': client.check_health(),
        'authentication': client.authentication_status().data,
    }
    await _send_event('/health', status)
    return status


@app.get('/accounts')
async def get_accounts() -> Any:
    data = client.portfolio_accounts().data
    await _send_event('/accounts', data)
    return data


@app.get('/ledger')
async def get_ledger(account_id: Optional[str] = None) -> Any:
    data = client.get_ledger(account_id=account_id).data
    await _send_event('/ledger', data)
    return data


@app.get('/portfolio/summary')
async def get_portfolio_summary(account_id: Optional[str] = None) -> Any:
    data = client.portfolio_summary(account_id=account_id).data
    await _send_event('/portfolio/summary', data)
    return data


@app.get('/accounts/{account_id}/positions/{conid}')
async def get_position_by_conid(account_id: str, conid: str) -> Any:
    data = client.positions_by_conid(account_id=account_id, conid=conid).data
    await _send_event('/accounts/{account_id}/positions/{conid}', data)
    return data


@app.get('/brokerage/accounts')
async def get_brokerage_accounts() -> Any:
    data = client.receive_brokerage_accounts().data
    await _send_event('/brokerage/accounts', data)
    return data


@app.get('/orders/live')
async def get_live_orders(
    filters: Optional[List[str]] = Query(default=None),
    force: Optional[bool] = None,
    account_id: Optional[str] = None,
) -> Any:
    data = client.live_orders(filters=filters, force=force, account_id=account_id).data
    await _send_event('/orders/live', data)
    return data


class PlaceOrderRequest(BaseModel):
    order_request: Union[Dict[str, Any], List[Dict[str, Any]]]
    answers: List[Dict[str, Any]]
    account_id: Optional[str] = None


@app.post('/orders')
async def place_order(body: PlaceOrderRequest) -> Any:
    data = client.place_order(
        order_request=body.order_request,
        answers=body.answers,
        account_id=body.account_id,
    ).data
    await _send_event('/orders', data)
    return data


if __name__ == '__main__':
    import uvicorn

    uvicorn.run(app, host='127.0.0.1', port=int(os.getenv('PORT', 8000)))
