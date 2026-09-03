// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./DeputyCenterLayout.sol";
import "../../nameserver/ownable/OwnableLogic.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "./DeputyCenterInterface.sol";

import "../helperLibrary/BytesLibrary.sol";
import "./DeputyCenterType.sol";

//import "hardhat/console.sol";

/****************************************/
/*
/* DeputyCenterLogic can only have ONE logic!!!!! Do not add delegate more than 1
/*
/****************************************/
//the deputy center records all unique nonce if used
contract DeputyCenterLogic is DeputyCenterLayout, OwnableLogic, NameServiceRefLogic, DeputyCenterInterface {

    //only this function will call anther contracts!!
    function dispatchTransactions(DeputyCenterType.BatchTransactions[] memory bTxs) override external {

        require(!_entryMark, "DeputyCenter, cannot dispatch transaction twice, even in a nest case");
        require(_barrierRecords.length == 0, "DeputyCenter, barrier array should be empty");
        require(_caller == address(0), "DeputyCenter, caller should be zero");
        _entryMark = true;
        _caller = msg.sender;

        require(_caller != address(0), "DeputyCenter, msg.sender should not be zero");

        for (uint256 i = 0; i < bTxs.length; i++) {
            require(bTxs[i].target != address(this), "DeputyCenter, can not dispatch transactions to self");
            (bool success, bytes memory ret) = bTxs[i].target.call{value : bTxs[i].value}(bTxs[i].callData);
            require(success, string(abi.encodePacked("dispatchTransactions [", i, "] fails: ", ret)));
        }

        while (_barrierRecords.length > 0) {
            DeputyCenterType.BarrierRecord memory barrierRecord = _barrierRecords[_barrierRecords.length - 1];
            require(_uniqueNonces[barrierRecord.signer][barrierRecord.barrierNonce], "DeputyCenter, barrier tx missing");
            _barrierRecords.pop();
        }

        _entryMark = false;
        _caller = address(0);
        require(_barrierRecords.length == 0, "DeputyCenter, barrier clear fails");
    }

    function calledAndCaller() override view external returns (bool called, address caller) {
        return (_entryMark, _caller);
    }

    //    receive() payable external {
    //        process();
    //    }
    //
    //    fallback() payable external {
    //        process();
    //    }

    //if parse success:
    //  return designatedSender as msgSender,
    //  return signer as signer (never be address(0))
    //if parse fails:
    //  return address(0) as msgSender,
    //  return address(0) as signer
    function process() internal
        //(
        //    address /* msgSender*/,
        //    address /* signer*/,
        //    uint256 /* value*/
        //)
    {
        //real abi-encoded calldata,various length,parsed by compiler with selector
        //real calldata length, uint256                                 -350, -318
        //to,address,20                                                 -318, -298
        //chainId,uint256                                               -298, -266
        //beforeTimeStamp(0),beforeBlockNumber(1),uint256,              -266, -234
        //uniqueNonce,uint256                                           -234, -202
        //dependUniqueNonce,uint256                                     -202, -170
        //barrierNonce,uint256                                          -170, -138
        //value,uint256                                                 -138, -106
        //onlyDesignatedTxOriginal,bool                                 -106, -105
        //designatedSender,address                                      -105, -85
        //signer, address                                               -85, -65
        //r,bytes32                                                     -65, -33
        //s,bytes32                                                     -33, -1
        //v,byte                                                        -1, -0

        /*
        use calldata and bytes slice to reduce gas,
        use memory to keep compatibility to old versions,
        */
        if (msg.data.length >= REAL_CALLDATA_LENGTH_FROM_RO) {

            bytes memory callData = msg.data;
            uint256 callDataLength = msg.data.length;

            //1, check length
            if (callDataLength - REAL_CALLDATA_LENGTH_FROM_RO != BytesLibrary.toUint256(callData, callDataLength - REAL_CALLDATA_LENGTH_FROM_RO)) {
                _return(address(0), address(0), 0, "deputy length error");
            }

            //2, check "to"
            {
                address to = BytesLibrary.toAddress(callData, callDataLength - TO_ADDRESS_FROM_RO);
                if (msg.sender/*the msg.sender is the 'to' contract*/ != to) {
                    _return(address(0), address(0), 0, "'to' address mismatch");
                }
            }

            //3, check chainId
            {
                uint256 chainId = BytesLibrary.toUint256(callData, callDataLength - CHAIN_ID_FROM_RO);
                if (chainId != block.chainid) {
                    _return(address(0), address(0), 0, "chain id mismatch");
                }
            }

            //4, check ttl
            {
                uint256 before = BytesLibrary.toUint256(callData, callDataLength - BEFORE_FROM_RO);
                if (before != uint256(0)) {

                    if (before >> 255 == uint256(0)) {
                        if (before > block.timestamp) {
                            _return(address(0), address(0), 0, "ttl, block timestamp");
                        }
                    } else {
                        if (
                            (before << 1) >> 1
                            > block.number
                        ) {
                            _return(address(0), address(0), 0, "ttl, block number");
                        }
                    }
                }
            }

            //5, recover signer and check
            address signer = address(0);
            {
                bytes memory toSign = BytesLibrary.slice(callData, 0, callDataLength - R_FROM_RO);
                bytes32 digest = keccak256(abi.encodePacked("\x19Ethereum Signed Message:\n32", keccak256(toSign)));
                bytes32 r = BytesLibrary.toBytes32(callData, callDataLength - R_FROM_RO);
                bytes32 s = BytesLibrary.toBytes32(callData, callDataLength - S_FROM_RO);
                uint8 v = BytesLibrary.toUint8(callData, callDataLength - V_FROM_RO);
                signer = ecrecover(digest, v, r, s);

                address presumedSigner = BytesLibrary.toAddress(callData, callDataLength - SIGNER_FROM_RO);
                if (presumedSigner != signer) {
                    _return(address(0), address(0), 0, "signature failure");
                }
            }

            //6, check and update unique nonce
            uint256 uniqueNonce = BytesLibrary.toUint256(callData, callDataLength - UNIQUE_NONCE_FROM_RO);
            {
                if (_uniqueNonces[signer][uniqueNonce]) {
                    _return(address(0), address(0), 0, "unique nonce");
                }
                _uniqueNonces[signer][uniqueNonce] = true;
            }

            //7, check depend nonce
            {
                uint256 dependUniqueNonce = BytesLibrary.toUint256(callData, callDataLength - DEPEND_UNIQUE_NONCE_FROM_RO);
                if (dependUniqueNonce != 0 && !_uniqueNonces[signer][dependUniqueNonce]) {
                    _return(address(0), address(0), 0, "depend nonce");
                }
            }

            //8, check barrier nonce
            {
                uint256 barrierNonce = BytesLibrary.toUint256(callData, callDataLength - BARRIER_NONCE_FROM_RO);
                if (barrierNonce != 0) {

                    if (!_entryMark) {
                        _return(address(0), address(0), 0, "barrier nonce, entry mark");
                    }

                    DeputyCenterType.BarrierRecord memory temp = DeputyCenterType.BarrierRecord({
                    signer : signer,
                    barrierNonce : barrierNonce
                    });

                    _barrierRecords.push(temp);
                }
            }

            //9, recover 'value'
            uint256 value = BytesLibrary.toUint256(callData, callDataLength - VALUE_FROM_RO);

            //10, recover 'designatedSender'
            address designatedSender = BytesLibrary.toAddress(callData, callDataLength - DESIGNATED_SENDER_FROM_RO);
            {
                uint8 onlyDesignatedTxOriginal = BytesLibrary.toUint8(callData, callDataLength - ONLY_DESIGNATED_TX_ORIGINAL_FROM_RO);
                if (onlyDesignatedTxOriginal != 0 && tx.origin != designatedSender) {
                    _return(address(0), address(0), 0, "onlyDesignatedTxOriginal");
                }
            }

            _return(designatedSender, signer, value, "");
        }

        _return(address(0), address(0), 0, "not enough deputy minimal length");
    }


    function _return(address designatedSender, address signer, uint256 value, string memory reason) pure internal {
        bytes memory returnData = abi.encode(designatedSender, signer, value, reason);

        assembly{
            let length := mload(returnData)
            return (add(returnData, 0x20), length)
        }
    }
}
