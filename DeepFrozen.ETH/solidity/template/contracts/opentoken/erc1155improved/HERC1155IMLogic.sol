// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC1155IMLayout.sol";
import "../../nameserver/ownable/OwnableLogic.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../../nameserver/reentrancy/ReentrancyLogic.sol";
import "../erc1155/HERC1155Logic.sol";
import "./HERC1155IMInterface.sol";

import "../../nameserver/nameService/NameServiceInterface.sol";
import "../assetVault/AssetVaultInterface.sol";

abstract contract HERC1155IMLogic is HERC1155IMLayout, OwnableLogic, NameServiceRefLogic, ReentrancyLogic, HERC1155Logic, HERC1155IMInterface {

    using SafeMath for uint256;
    using Address for address;

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoMint(){
        _sudoMint = true;
        _;
        _sudoMint = false;
    }

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoBurn(){
        _sudoBurn = true;
        _;
        _sudoBurn = false;
    }

    //for non-sudo mint, the "to" must be the msg.sender
    function _mintNormal(uint256 id, uint256 amount) internal onlyOnce {
        _mint(msg.sender, id, amount, "");
    }

    //for non-sudo mint, the "to" must be the msg.sender
    function _mintNormal(uint256 id, uint256 amount, bytes memory data) internal onlyOnce {
        _mint(msg.sender, id, amount, data);
    }

    function _mintSudo(address to, uint256 id, uint256 amount) internal enableSudoMint onlyOnce {
        _mint(to, id, amount, "");
    }

    function _mintSudo(address to, uint256 id, uint256 amount, bytes memory data) internal enableSudoMint onlyOnce {
        _mint(to, id, amount, data);
    }

    //==

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnNormal(uint256 id, uint256 amount) internal onlyOnce {
        _burn(msg.sender, id, amount);
    }

    function _burnSudo(address from, uint256 id, uint256 amount) internal enableSudoBurn onlyOnce {
        _burn(from, id, amount);
    }

    function setAccessControl(address accessControl_) override external onlyOwner {
        _setAccessControl(accessControl_);
    }

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) override external onlyOwner {
        _supportTransfer = supportTransfer_;
        _supportMint = supportMint_;
        _supportBurn = supportBurn_;
    }

    function setBlockListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _blockListFrom[from[i]] = flag;
        }
    }

    function setBlockListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _blockListTo[to[i]] = flag;
        }
    }

    function setPrivilegeListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _privilegeListFrom[from[i]] = flag;
        }
    }

    function setPrivilegeListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _privilegeListTo[to[i]] = flag;
        }
    }
    //==========

    function support() override view external returns (bool supportTransfer, bool supportMint, bool supportBurn){
        return (_supportTransfer, _supportMint, _supportBurn);
    }

    function transferTxs() override view external returns (uint256) {
        return _transferTxs;
    }

    function transferAmounts() override view external returns (uint256) {
        return _transferAmounts;
    }

    function interactAmounts() override view external returns (uint256){
        return _interactAmounts;
    }

    function isInteracted(address who) override view external returns (bool){
        return _interactAccount[who];
    }

    function _beforeTokenTransfer(
        address operator,
        address from,
        address to,
        uint256[] memory ids,
        uint256[] memory amounts,
        bytes memory data
    )
    override(HERC1155Logic)
    virtual
    internal
    {

        NameServiceInterface ns = nsUnsafe();

        if (from == address(0) && to != address(0)) {
            //mint mode
            //some how you call _mint :) , you must have done job of mint permission
            _beforeTokenTransferMint(to, ids, amounts, address(ns));
        } else if (from != address(0) && to == address(0)) {
            //burn mode
            //some how you call _burn :) , you must have done job of burn permission
            _beforeTokenTransferBurn(from, ids, amounts, address(ns));
        } else if (from != address(0) && to != address(0)) {
            //normal transfer
            _beforeTokenTransferTransfer(from, to, ids, amounts, address(ns));
        } else {
            revert("transfer, 'from' and 'to' should never be address(0) at same time");
        }


        //impossible to hit the 2^256 :)
    unchecked{_transferTxs = _transferTxs + 1;}

        for (uint256 i = 0; i < amounts.length; i++) {
            bool flag;
            (flag, _transferAmounts) = _transferAmounts.tryAdd(amounts[i]);
            if (flag) {
                _transferAmounts = type(uint256).max;
            }
        }

        if (!_interactAccount[from]) {
            _interactAccount[from] = true;
        unchecked{_interactAmounts = _interactAmounts.add(1);}
        }

        HERC1155Logic._beforeTokenTransfer(operator, from, to, ids, amounts, data);
    }

    function _beforeTokenTransferMint(address to, uint256[] memory, uint256[] memory, address ns) view internal {

        //not support mint
        if (_sudoMint) {
            //pass
        } else {
            if (ns != address(0) && NameServiceInterface(ns).isPrivileged(to)) {
                //privilege pass
            } else if (_privilegeListTo[to]) {
                //privilege pass
            } else if (ns != address(0) && NameServiceInterface(ns).isBlocked(to)) {
                revert("ac.isBlocked-to");
            } else if (_blockListTo[to]) {
                revert("blockListTo");
            } else {
                if (_supportMint) {

                } else {
                    revert("un-support mint");
                }
            }
        }
    }

    function _beforeTokenTransferBurn(address from, uint256[] memory, uint256[] memory, address ns) view internal {

        if (_sudoBurn) {
            //pass
        } else {
            if (ns != address(0) && NameServiceInterface(ns).isPrivileged(from)) {
                //privilege overrides blockList
            } else if (_privilegeListFrom[from]) {
                //privilege overrides blockList
            } else if (ns != address(0) && NameServiceInterface(ns).isBlocked(from)) {
                revert("ac.isBlocked-from");
            } else if (_blockListFrom[from]) {
                revert("blockListFrom");
            } else {
                if (_supportBurn) {

                } else {
                    revert("un-support burn");
                }
            }
        }
    }

    function _beforeTokenTransferTransfer(address from, address to, uint256[] memory, uint256[] memory, address ns) view internal {

        if (ns != address(0) &&
            (NameServiceInterface(ns).isPrivileged(from) || NameServiceInterface(ns).isPrivileged(to))
        ) {
            //privilege overrides blockList
        } else if (_privilegeListFrom[from] || _privilegeListTo[to]) {
            //privilege overrides blockList
        } else if (ns != address(0) &&
            (NameServiceInterface(ns).isBlocked(from) || NameServiceInterface(ns).isBlocked(to))
        ) {
            revert("ac.isBlocked-from||ac.isBlocked-to");
        } else if (_blockListFrom[from] || _blockListTo[to]) {
            revert("blockListFrom||blockListTo");
        } else {
            if (_supportTransfer) {

            } else {
                revert("un-support transfer");
            }
        }
    }
}

