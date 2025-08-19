import os
from datetime import datetime
from pathlib import Path
import sys
import traceback
from typing import Any, Dict, List, Optional, Union, Set

from fastapi import FastAPI, Query, WebSocket, WebSocketDisconnect
from fastapi.encoders import jsonable_encoder
from fastapi.responses import HTMLResponse
from pydantic import BaseModel

from ibind import Answers, IbkrClient, OrderRequest, StockQuery, ibind_logs_initialize

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
        except Exception as e:
            # print a one-liner with the exception message
            print(f"Error sending message to {ws!r}: {e}", file=sys.stderr)
            # print the full traceback for more context
            traceback.print_exc(file=sys.stderr)
            # dead.append(ws)
    for ws in dead:
        _connections.discard(ws)


async def _send_event(endpoint: str, data: Any, req: Optional[Any] = None) -> None:
    await _broadcast({'time': datetime.now().strftime('%Y-%m-%d %#I:%M:%S %p'), 'endpoint': endpoint, 'data': jsonable_encoder(data), 'req': jsonable_encoder(req) if req is not None else None})


@app.on_event('startup')
async def startup_event() -> None:
    accounts = client.portfolio_accounts().data
    if accounts:
        client.account_id = accounts[0].get('accountId')
    client.receive_brokerage_accounts()
    await _send_event('startup', accounts)
    await get_health()


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
    return status


@app.get('/portfolio/accounts')
async def get_accounts() -> Any:
    data = client.portfolio_accounts().data
    await _send_event('/portfolio/accounts', data)
    return data


@app.get('/ledger')
async def get_ledger(account_id: Optional[str] = None) -> Any:
    print(f"Received {account_id}")
    data = client.get_ledger(account_id=account_id).data
    await _send_event('/ledger', data, {'account_id': account_id})
    return data


@app.get('/portfolio/{accountId}/summary')
async def get_portfolio_summary(account_id: Optional[str] = None) -> Any:
    print(f"Received {account_id}")
    data = client.portfolio_summary(account_id=account_id).data
    await _send_event('/portfolio/summary', data, {'account_id': account_id})
    return data


@app.get('/portfolio/positions/{conid}')
async def position_and_contract_info(conid: str) -> Any:
    print(f"Received {conid}")
    data = client.position_and_contract_info(conid=conid).data
    await _send_event('/portfolio/positions/{conid}', data, {'conid': conid})
    return data


@app.get('/brokerage/accounts')
async def get_brokerage_accounts() -> Any:
    data = client.receive_brokerage_accounts().data
    await _send_event('/brokerage/accounts', data)
    return data

class SearchSymbolRequest(BaseModel):
    name: Optional[bool] = None
    symbol: str
    sec_type: Optional[str] = None

@app.post('/iserver/secdef/search')
async def search_contract_by_symbol(body: SearchSymbolRequest) -> Any:
    print(f"Received {body}")
    data = client.search_contract_by_symbol(
            symbol=body.symbol,
        	sec_type=body.sec_type,
        	name=body.name).data
    await _send_event('/iserver/secdef/search', data, {'symbol': body.symbol, 'sec_type': body.sec_type, 'name': body.name})
    return data


@app.get('/trsrv/stocks')
async def security_stocks_by_symbol(symbols: str = Query(
        default=None,
        description="Single comma-separated list (?symbols=AAPL,MSFT)"
    )) -> Any:
    print(f"Received {symbols}")
    
    instrument_conditions = {"assetClass": "STK"}
    contract_conditions = {"isUS": True}

    queries = [
        StockQuery(
            symbol=s,
            instrument_conditions=instrument_conditions,
            contract_conditions=contract_conditions,
        )
        for s in symbols.split(",")
    ]
    
    data = client.security_stocks_by_symbol(queries, default_filtering=False).data
    await _send_event('/trsrv/stocks', data, {'symbols': symbols})
    return data


class RulesEnvelope(BaseModel):
    conid: str
    
@app.post('/iserver/contract/rules')
async def search_contract_rules(body: RulesEnvelope) -> Any:
    print(f"Received {body}")
    data = client.search_contract_rules(body.conid).data
    await _send_event('/iserver/contract/rules', data, {'rules': body})
    return data


@app.get('/iserver/marketdata/snapshot')
async def get_live_orders(
    conids: Optional[List[str]] = Query(default=None),
    fields: Optional[List[str]] = Query(default=None)
) -> Any:
    print(f"Received {conids} | {fields}")
    data = client.live_marketdata_snapshot(conids, fields).data
    await _send_event('/iserver/marketdata/snapshot', data, {'conids': conids, 'fields': fields})
    return data


@app.get('/portfolio/{account_id}/position/{conid}')
async def positions_by_conid(account_id: str, conid: str) -> Any:
    print(f"Received {account_id} | {conid}")
    data = client.positions_by_conid(account_id, conid).data
    await _send_event('/portfolio/{account_id}/position/{conid}', data, {'account_id': account_id, 'conid': conid})
    return data


@app.get('/iserver/account/orders')
async def get_live_orders(
    filters: Optional[List[str]] = Query(default=None),
    force: Optional[bool] = Query(default=None),
    account_id: Optional[str] = Query(default=None),
) -> Any:
    print(f"Received {filters} | {force} | {account_id}")
    data = client.live_orders(filters=filters, force=force, account_id=account_id).data
    await _send_event('/iserver/account/orders', data, {'filters': filters, 'force': force, 'account_id': account_id})
    return data


class OrderEnvelope(BaseModel):
    orders: List[OrderRequest]
    answers: Answers
    

@app.post('/iserver/account/{accountid}/orders/whatif')
async def whatif_order(accountid: str, body: OrderEnvelope) -> Any:
    print(f"Received {accountid} | {body}")
    data = client.whatif_order(body.orders[0], accountid).data
    await _send_event('/whatif', data, {'order': body})
    return data


@app.post('/iserver/account/{accountid}/orders')
async def place_order(accountid: str, body: OrderEnvelope) -> Any:
    print(f"Received {accountid} | {body}")
    data = client.place_order(body.orders, body.answers, account_id=accountid).data
    await _send_event('/iserver/account/{accountid}/orders', data, {'order': body})
    return data


if __name__ == '__main__':
    import uvicorn

    uvicorn.run(app, host='127.0.0.1', port=int(os.getenv('PORT', 8000)), ws_max_size=64 * 1024 * 1024)
