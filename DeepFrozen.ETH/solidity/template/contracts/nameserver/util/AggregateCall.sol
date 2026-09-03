// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract AggregateCall {
    struct Call {
        address target;
        bytes callData;
    }

    struct Return {
        bool success;
        bytes returnData;
    }

    function aggregate(Call[] memory calls) public returns (uint256 blockNumber, bytes[] memory returnData) {
        blockNumber = block.number;
        returnData = new bytes[](calls.length);
        for (uint256 i = 0; i < calls.length; i++) {
            (bool success, bytes memory ret) = calls[i].target.call(calls[i].callData);
            require(success, string(abi.encodePacked("aggregate revert: ", ret)));
            returnData[i] = ret;
        }
    }

    function aggregateStatic(Call[] memory calls) view public returns (uint256 blockNumber, bytes[] memory returnData) {
        blockNumber = block.number;
        returnData = new bytes[](calls.length);
        for (uint256 i = 0; i < calls.length; i++) {
            (bool success, bytes memory ret) = calls[i].target.staticcall(calls[i].callData);
            require(success, string(abi.encodePacked("aggregate revert: ", ret)));
            returnData[i] = ret;
        }
    }

    function aggregateDetail(Call[] memory calls) public returns (uint256 blockNumber, Return[] memory returnData) {
        blockNumber = block.number;
        returnData = new Return[](calls.length);
        for (uint256 i = 0; i < calls.length; i++) {
            (bool success, bytes memory ret) = calls[i].target.call(calls[i].callData);
            returnData[i].success = success;
            returnData[i].returnData = ret;
        }
    }

    function aggregateStaticDetail(Call[] memory calls) view public returns (uint256 blockNumber, Return[] memory returnData) {
        blockNumber = block.number;
        returnData = new Return[](calls.length);
        for (uint256 i = 0; i < calls.length; i++) {
            (bool success, bytes memory ret) = calls[i].target.staticcall(calls[i].callData);
            returnData[i].success = success;
            returnData[i].returnData = ret;
        }
    }

    // Helper functions
    function getEthBalance(address addr) public view returns (uint256 balance) {
        balance = addr.balance;
    }

    function getBlockHash(uint256 blockNumber) public view returns (bytes32 blockHash) {
        blockHash = blockhash(blockNumber);
    }

    function getLastBlockHash() public view returns (bytes32 blockHash) {
        blockHash = blockhash(block.number - 1);
    }

    function getCurrentBlockTimestamp() public view returns (uint256 timestamp) {
        timestamp = block.timestamp;
    }

    function getCurrentBlockDifficulty() public view returns (uint256 difficulty) {
        difficulty = block.difficulty;
    }

    function getCurrentBlockGasLimit() public view returns (uint256 gaslimit) {
        gaslimit = block.gaslimit;
    }

    function getCurrentBlockCoinbase() public view returns (address coinbase) {
        coinbase = block.coinbase;
    }
}
